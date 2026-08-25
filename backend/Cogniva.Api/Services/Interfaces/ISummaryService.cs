namespace Cogniva.Api.Services.Interfaces;

public interface ISummaryService
{
    Task<string> GenerateAsync(
        string documentName,
        IReadOnlyList<string> chunks,
        CancellationToken cancellationToken = default);
}
