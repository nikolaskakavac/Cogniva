namespace Cogniva.Api.Models;

public sealed record RagResult(string Answer, IReadOnlyList<SemanticSearchResult> Sources);
