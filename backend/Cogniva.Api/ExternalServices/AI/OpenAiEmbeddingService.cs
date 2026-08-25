using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Cogniva.Api.Configuration;
using Cogniva.Api.Models.Processing;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.ExternalServices.AI;

public sealed class OpenAiEmbeddingService(
    HttpClient httpClient,
    IOptions<AiOptions> options,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    private readonly AiOptions _options = options.Value;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await GenerateEmbeddingsAsync([text], cancellationToken);
        return result[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0) return [];
        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        request.Content = JsonContent.Create(new EmbeddingRequest(
            _options.EmbeddingModel,
            texts,
            _options.EmbeddingDimensions));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DocumentProcessingException("Embedding servis trenutno nije dostupan.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Embedding provider request failed");
            throw new DocumentProcessingException("Embedding servis trenutno nije dostupan.");
        }

        using (response)
        {
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Embedding provider returned status {StatusCode}", (int)response.StatusCode);
            throw new DocumentProcessingException("Embedding servis trenutno nije dostupan.");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new DocumentProcessingException("Embedding servis je vratio neispravan odgovor.");
        var ordered = payload.Data.OrderBy(item => item.Index).Select(item => item.Embedding).ToList();

        if (ordered.Count != texts.Count || ordered.Any(vector => vector.Length != _options.EmbeddingDimensions))
        {
            throw new DocumentProcessingException("Embedding servis je vratio vektore neočekivane dimenzije.");
        }

        return ordered;
        }
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input,
        [property: JsonPropertyName("dimensions")] int Dimensions);

    private sealed record EmbeddingResponse([property: JsonPropertyName("data")] IReadOnlyList<EmbeddingItem> Data);
    private sealed record EmbeddingItem(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
