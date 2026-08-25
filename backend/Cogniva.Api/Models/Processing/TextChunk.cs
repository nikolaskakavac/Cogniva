namespace Cogniva.Api.Models.Processing;

public sealed record TextChunk(string Content, int ChunkIndex, int? PageNumber);
