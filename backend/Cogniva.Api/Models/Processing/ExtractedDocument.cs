namespace Cogniva.Api.Models.Processing;

public sealed record ExtractedDocument(IReadOnlyList<ExtractedSection> Sections);
