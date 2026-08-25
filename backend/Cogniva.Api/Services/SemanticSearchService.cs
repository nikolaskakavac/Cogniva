using Cogniva.Api.Configuration;
using Cogniva.Api.Data;
using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Middleware;
using Cogniva.Api.Models;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Cogniva.Api.Services;

public sealed class SemanticSearchService(
    AppDbContext dbContext,
    IEmbeddingService embeddingService,
    IOptions<AiOptions> aiOptions,
    IOptions<RagOptions> ragOptions,
    ILogger<SemanticSearchService> logger) : ISemanticSearchService
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly RagOptions _ragOptions = ragOptions.Value;

    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        Guid userId,
        IReadOnlyCollection<Guid> documentIds,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count == 0 || string.IsNullOrWhiteSpace(query)) return [];

        float[] queryEmbedding;
        try
        {
            queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query.Trim(), cancellationToken);
        }
        catch (DocumentProcessingException exception)
        {
            logger.LogWarning(exception, "Query embedding generation failed");
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "Semantic pretraga trenutno nije dostupna.",
                exception.Message);
        }

        if (queryEmbedding.Length != _aiOptions.EmbeddingDimensions)
        {
            throw DimensionMismatch();
        }

        var selectedIds = documentIds.Distinct().ToArray();
        var storedEmbedding = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => selectedIds.Contains(chunk.DocumentId)
                && chunk.Document.UserId == userId
                && chunk.Document.Status == DocumentStatus.Ready
                && chunk.Embedding != null)
            .Select(chunk => chunk.Embedding)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedEmbedding is null) return [];
        if (storedEmbedding.ToArray().Length != queryEmbedding.Length) throw DimensionMismatch();

        var queryVector = new Vector(queryEmbedding);
        var limit = Math.Clamp(topK, 1, 20);

        return await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => selectedIds.Contains(chunk.DocumentId)
                && chunk.Document.UserId == userId
                && chunk.Document.Status == DocumentStatus.Ready
                && chunk.Embedding != null)
            .Select(chunk => new
            {
                Chunk = chunk,
                DocumentName = chunk.Document.OriginalFileName,
                Distance = chunk.Embedding!.CosineDistance(queryVector)
            })
            .Where(result => result.Distance <= _ragOptions.MaxCosineDistance)
            .OrderBy(result => result.Distance)
            .Take(limit)
            .Select(result => new SemanticSearchResult(
                result.Chunk.Id,
                result.Chunk.DocumentId,
                result.DocumentName,
                result.Chunk.Content,
                result.Chunk.ChunkIndex,
                result.Chunk.PageNumber,
                result.Distance))
            .ToListAsync(cancellationToken);
    }

    private static ApiException DimensionMismatch() => new(
        StatusCodes.Status409Conflict,
        "Embedding modeli nisu usklađeni.",
        "Dokumenti moraju biti ponovo obrađeni trenutnim embedding modelom.");
}
