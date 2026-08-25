using Cogniva.Api.Data;
using Cogniva.Api.DTOs.Conversations;
using Cogniva.Api.DTOs.Dashboard;
using Cogniva.Api.DTOs.Documents;
using Cogniva.Api.Models;
using Cogniva.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cogniva.Api.Services;

public sealed class DashboardService(AppDbContext dbContext) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var documents = dbContext.Documents.AsNoTracking().Where(item => item.UserId == userId);
        var conversations = dbContext.Conversations.AsNoTracking().Where(item => item.UserId == userId);

        var documentCount = await documents.CountAsync(cancellationToken);
        var readyDocumentCount = await documents.CountAsync(item => item.Status == DocumentStatus.Ready, cancellationToken);
        var conversationCount = await conversations.CountAsync(cancellationToken);
        var recentDocuments = await documents.OrderByDescending(item => item.UploadedAt).Take(3)
            .Select(item => new DocumentListItemResponse(
                item.Id, item.Name, item.OriginalFileName, item.FileType, item.Status.ToString(), item.UploadedAt, item.ProcessedAt))
            .ToListAsync(cancellationToken);
        var recentConversations = await conversations.OrderByDescending(item => item.UpdatedAt).Take(3)
            .Select(item => new ConversationListItemResponse(
                item.Id, item.Title, item.CreatedAt, item.UpdatedAt, item.Messages.Count,
                item.ConversationDocuments.OrderBy(link => link.Document.OriginalFileName)
                    .Select(link => link.Document.OriginalFileName).ToList()))
            .ToListAsync(cancellationToken);

        return new DashboardResponse(documentCount, readyDocumentCount, conversationCount, recentDocuments, recentConversations);
    }
}
