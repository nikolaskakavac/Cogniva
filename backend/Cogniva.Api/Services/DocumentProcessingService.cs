using Cogniva.Api.Configuration;
using Cogniva.Api.Data;
using Cogniva.Api.DTOs.Documents;
using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Middleware;
using Cogniva.Api.Models;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;

namespace Cogniva.Api.Services;

public sealed class DocumentProcessingService(
    AppDbContext dbContext,
    ITextExtractionService textExtractionService,
    IChunkingService chunkingService,
    IEmbeddingService embeddingService,
    IDocumentService documentService,
    IWebHostEnvironment environment,
    IOptions<FileStorageOptions> storageOptions,
    IOptions<AiOptions> aiOptions,
    ILogger<DocumentProcessingService> logger) : IDocumentProcessingService
{
    private readonly FileStorageOptions _storageOptions = storageOptions.Value;
    private readonly AiOptions _aiOptions = aiOptions.Value;

    public async Task<DocumentDetailsResponse> ProcessDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Id == documentId,
            cancellationToken) ?? throw new ApiException(
                StatusCodes.Status404NotFound,
                "Dokument nije pronađen.",
                "Traženi dokument ne postoji.");

        if (document.Status == DocumentStatus.Processing)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Obrada je već pokrenuta.",
                "Sačekajte da se trenutna obrada dokumenta završi.");
        }

        document.Status = DocumentStatus.Processing;
        document.ProcessingError = null;
        document.ProcessedAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var physicalPath = ResolvePhysicalPath(document.FilePath);
            if (!File.Exists(physicalPath))
            {
                throw new DocumentProcessingException("Fizički fajl dokumenta nije pronađen.");
            }

            var extracted = await textExtractionService.ExtractAsync(physicalPath, document.FileType, cancellationToken);
            var chunks = chunkingService.CreateChunks(extracted);
            if (chunks.Count == 0)
            {
                throw new DocumentProcessingException("Dokument ne sadrži dovoljno teksta za obradu.");
            }

            var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                chunks.Select(chunk => chunk.Content).ToList(),
                cancellationToken);

            if (embeddings.Count != chunks.Count
                || embeddings.Any(embedding => embedding.Length != _aiOptions.EmbeddingDimensions))
            {
                throw new DocumentProcessingException("Generisani embedding vektori nisu ispravni.");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var oldChunks = await dbContext.DocumentChunks
                .Where(chunk => chunk.DocumentId == document.Id)
                .ToListAsync(cancellationToken);
            dbContext.DocumentChunks.RemoveRange(oldChunks);

            dbContext.DocumentChunks.AddRange(chunks.Select((chunk, index) => new DocumentChunk
            {
                DocumentId = document.Id,
                Content = chunk.Content,
                ChunkIndex = index,
                PageNumber = chunk.PageNumber,
                Embedding = new Vector(embeddings[index])
            }));

            document.Status = DocumentStatus.Ready;
            document.ProcessingError = null;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Document processing failed for document {DocumentId}", documentId);
            dbContext.ChangeTracker.Clear();
            var failedDocument = await dbContext.Documents.SingleAsync(
                item => item.UserId == userId && item.Id == documentId,
                CancellationToken.None);
            failedDocument.Status = DocumentStatus.Failed;
            failedDocument.ProcessedAt = null;
            failedDocument.ProcessingError = exception is DocumentProcessingException
                ? exception.Message
                : "Došlo je do neočekivane greške tokom obrade dokumenta.";
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        return await documentService.GetDocumentAsync(userId, documentId, CancellationToken.None);
    }

    private string ResolvePhysicalPath(string storedFileName)
    {
        var root = Path.GetFullPath(Path.IsPathRooted(_storageOptions.UploadPath)
            ? _storageOptions.UploadPath
            : Path.Combine(environment.ContentRootPath, _storageOptions.UploadPath));
        var rootWithSeparator = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentProcessingException("Putanja dokumenta nije ispravna.");
        }
        return candidate;
    }
}
