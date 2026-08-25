using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Cogniva.Api.Configuration;
using Cogniva.Api.Middleware;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.ExternalServices.AI;

public sealed class OpenAiCompatibleLlmService(
    HttpClient httpClient,
    IOptions<AiOptions> options,
    ILogger<OpenAiCompatibleLlmService> logger) : ILlmService
{
    private readonly AiOptions _options = options.Value;

    public async Task<string> GenerateResponseAsync(
        IReadOnlyList<LlmChatMessage> messages,
        CancellationToken cancellationToken = default,
        int maxTokens = 1024,
        string? model = null)
    {
        var selectedModel = string.IsNullOrWhiteSpace(model) ? _options.ChatModel : model.Trim();
        if (string.IsNullOrWhiteSpace(selectedModel))
        {
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "AI model nije konfigurisan.",
                "Podesite chat model pre pokretanja AI razgovora.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        request.Content = JsonContent.Create(new ChatCompletionRequest(
            selectedModel,
            messages.Select(message => new ChatCompletionMessage(message.Role, message.Content)).ToList(),
            0.1,
            maxTokens,
            "none"));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("LLM provider returned status {StatusCode}", (int)response.StatusCode);
                throw ProviderUnavailable();
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
            var content = payload?.Choices.FirstOrDefault()?.Message.Content?.Trim();
            return !string.IsNullOrWhiteSpace(content)
                ? content
                : throw new ApiException(
                    StatusCodes.Status502BadGateway,
                    "AI servis je vratio neispravan odgovor.",
                    "Nije moguće generisati odgovor. Pokušajte ponovo.");
        }
        catch (ApiException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw ProviderUnavailable();
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "LLM provider request failed");
            throw ProviderUnavailable();
        }
    }

    private static ApiException ProviderUnavailable() => new(
        StatusCodes.Status503ServiceUnavailable,
        "AI servis trenutno nije dostupan.",
        "Proverite da li je lokalni AI servis pokrenut i da li je chat model instaliran.");

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatCompletionMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("reasoning_effort")] string ReasoningEffort);

    private sealed record ChatCompletionMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice> Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatCompletionMessage Message);
}
