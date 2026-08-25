using Cogniva.Api.Models.Processing;

namespace Cogniva.Api.Services.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<TextChunk> CreateChunks(ExtractedDocument document);
}
