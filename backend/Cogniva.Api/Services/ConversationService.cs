using Cogniva.Api.Configuration;
using Cogniva.Api.Data;
using Cogniva.Api.DTOs.Conversations;
using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Middleware;
using Cogniva.Api.Models;
using Cogniva.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Services;

public sealed class ConversationService(
    AppDbContext dbContext,
    IRagService ragService,
    IOptions<RagOptions> ragOptions) : IConversationService
{
    private readonly RagOptions _ragOptions = ragOptions.Value;

    public async Task<ConversationDetailsResponse> CreateAsync(
        Guid userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var documentIds = request.DocumentIds.Distinct().ToArray();
        if (documentIds.Length == 0)
        {
            throw new ApiException(400, "Dokument nije izabran.", "Izaberite najmanje jedan dokument.");
        }

        var ownedDocuments = await dbContext.Documents
            .Where(document => document.UserId == userId && documentIds.Contains(document.Id))
            .Select(document => new { document.Id, document.Status })
            .ToListAsync(cancellationToken);
        if (ownedDocuments.Count != documentIds.Length) throw DocumentNotFound();
        if (ownedDocuments.Any(document => document.Status != DocumentStatus.Ready))
        {
            throw new ApiException(409, "Dokument nije spreman.", "Izabrani dokument nije spreman za AI analizu.");
        }

        var conversation = new Conversation
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Novi razgovor" : request.Title.Trim(),
            ConversationDocuments = documentIds.Select(documentId => new ConversationDocument
            {
                DocumentId = documentId
            }).ToList()
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, conversation.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationListItemResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Select(conversation => new ConversationListItemResponse(
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt,
                conversation.UpdatedAt,
                conversation.Messages.Count,
                conversation.ConversationDocuments
                    .OrderBy(link => link.Document.OriginalFileName)
                    .Select(link => link.Document.OriginalFileName)
                    .ToList()))
            .ToListAsync(cancellationToken);

    public async Task<ConversationDetailsResponse> GetAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.Id == conversationId && item.UserId == userId)
            .Select(item => new ConversationDetailsResponse(
                item.Id,
                item.Title,
                item.CreatedAt,
                item.UpdatedAt,
                item.ConversationDocuments.OrderBy(link => link.Document.OriginalFileName)
                    .Select(link => new ConversationDocumentResponse(
                        link.Document.Id, link.Document.Name, link.Document.OriginalFileName)).ToList(),
                item.Messages.OrderBy(message => message.CreatedAt)
                    .Select(message => new MessageResponse(
                        message.Id,
                        message.Role.ToString(),
                        message.Content,
                        message.CreatedAt,
                        message.Sources.OrderByDescending(source => source.RelevanceScore)
                            .Select(source => new MessageSourceResponse(
                                source.DocumentId,
                                source.DocumentChunkId,
                                source.Document.OriginalFileName,
                                source.DocumentChunk.ChunkIndex,
                                source.DocumentChunk.PageNumber,
                                source.DocumentChunk.Content.Length > 180
                                    ? source.DocumentChunk.Content.Substring(0, 180) + "…"
                                    : source.DocumentChunk.Content,
                                source.RelevanceScore)).ToList())).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return conversation ?? throw ConversationNotFound();
    }

    public async Task<MessageResponse> SendMessageAsync(
        Guid userId,
        Guid conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ApiException(400, "Poruka je prazna.", "Unesite pitanje pre slanja.");
        }

        var conversation = await dbContext.Conversations
            .Include(item => item.ConversationDocuments)
            .ThenInclude(link => link.Document)
            .SingleOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId, cancellationToken)
            ?? throw ConversationNotFound();
        if (conversation.ConversationDocuments.Any(link => link.Document.Status != DocumentStatus.Ready))
        {
            throw new ApiException(409, "Dokument nije spreman.", "Izabrani dokument nije spreman za AI analizu.");
        }

        var previousMessages = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(_ragOptions.HistoryMessageLimit)
            .OrderBy(message => message.CreatedAt)
            .Select(message => new LlmChatMessage(
                message.Role == MessageRole.User ? "user" : "assistant",
                message.Content))
            .ToListAsync(cancellationToken);

        var userMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content
        };
        dbContext.Messages.Add(userMessage);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var documentIds = conversation.ConversationDocuments.Select(link => link.DocumentId).ToArray();
        var ragResult = await ragService.AnswerAsync(
            userId, documentIds, content, previousMessages, cancellationToken);

        var assistantMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = ragResult.Answer,
            Sources = ragResult.Sources.Select(source => new MessageSource
            {
                DocumentId = source.DocumentId,
                DocumentChunkId = source.DocumentChunkId,
                RelevanceScore = source.RelevanceScore
            }).ToList()
        };
        dbContext.Messages.Add(assistantMessage);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return await dbContext.Messages.AsNoTracking()
            .Where(message => message.Id == assistantMessage.Id)
            .Select(message => new MessageResponse(
                message.Id,
                message.Role.ToString(),
                message.Content,
                message.CreatedAt,
                message.Sources.OrderByDescending(source => source.RelevanceScore)
                    .Select(source => new MessageSourceResponse(
                        source.DocumentId,
                        source.DocumentChunkId,
                        source.Document.OriginalFileName,
                        source.DocumentChunk.ChunkIndex,
                        source.DocumentChunk.PageNumber,
                        source.DocumentChunk.Content.Length > 180
                            ? source.DocumentChunk.Content.Substring(0, 180) + "…"
                            : source.DocumentChunk.Content,
                        source.RelevanceScore)).ToList()))
            .SingleAsync(CancellationToken.None);
    }

    public async Task<MessageResponse> RetryMessageAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .Include(item => item.ConversationDocuments)
            .ThenInclude(link => link.Document)
            .SingleOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId, cancellationToken)
            ?? throw ConversationNotFound();

        var message = await dbContext.Messages.SingleOrDefaultAsync(
            item => item.Id == messageId && item.ConversationId == conversationId,
            cancellationToken) ?? throw MessageNotFound();
        var latestMessageId = await dbContext.Messages
            .Where(item => item.ConversationId == conversationId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => item.Id)
            .FirstAsync(cancellationToken);
        EnsureRetryable(message.Role, message.Id, latestMessageId);
        if (conversation.ConversationDocuments.Any(link => link.Document.Status != DocumentStatus.Ready))
        {
            throw new ApiException(409, "Dokument nije spreman.", "Izabrani dokument nije spreman za AI analizu.");
        }

        var history = await dbContext.Messages.AsNoTracking()
            .Where(item => item.ConversationId == conversationId && item.CreatedAt < message.CreatedAt)
            .OrderByDescending(item => item.CreatedAt)
            .Take(_ragOptions.HistoryMessageLimit)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new LlmChatMessage(
                item.Role == MessageRole.User ? "user" : "assistant", item.Content))
            .ToListAsync(cancellationToken);

        var result = await ragService.AnswerAsync(
            userId,
            conversation.ConversationDocuments.Select(link => link.DocumentId).ToArray(),
            message.Content,
            history,
            cancellationToken);
        var assistantMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = result.Answer,
            Sources = result.Sources.Select(source => new MessageSource
            {
                DocumentId = source.DocumentId,
                DocumentChunkId = source.DocumentChunkId,
                RelevanceScore = source.RelevanceScore
            }).ToList()
        };
        dbContext.Messages.Add(assistantMessage);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return await dbContext.Messages.AsNoTracking()
            .Where(item => item.Id == assistantMessage.Id)
            .Select(item => new MessageResponse(
                item.Id, item.Role.ToString(), item.Content, item.CreatedAt,
                item.Sources.OrderByDescending(source => source.RelevanceScore)
                    .Select(source => new MessageSourceResponse(
                        source.DocumentId,
                        source.DocumentChunkId,
                        source.Document.OriginalFileName,
                        source.DocumentChunk.ChunkIndex,
                        source.DocumentChunk.PageNumber,
                        source.DocumentChunk.Content.Length > 180
                            ? source.DocumentChunk.Content.Substring(0, 180) + "…"
                            : source.DocumentChunk.Content,
                        source.RelevanceScore)).ToList()))
            .SingleAsync(CancellationToken.None);
    }

    private static ApiException ConversationNotFound() => new(404, "Razgovor nije pronađen.", "Traženi razgovor ne postoji.");
    private static ApiException DocumentNotFound() => new(404, "Dokument nije pronađen.", "Traženi dokument ne postoji.");
    private static ApiException MessageNotFound() => new(404, "Poruka nije pronađena.", "Tražena poruka ne postoji.");

    internal static void EnsureRetryable(MessageRole role, Guid messageId, Guid latestMessageId)
    {
        if (role != MessageRole.User)
        {
            throw new ApiException(400, "Poruku nije moguće ponoviti.", "Samo korisnička poruka bez odgovora može biti ponovljena.");
        }
        if (messageId != latestMessageId)
        {
            throw new ApiException(409, "Odgovor već postoji.", "Ovu poruku nije potrebno ponovo slati.");
        }
    }
}
