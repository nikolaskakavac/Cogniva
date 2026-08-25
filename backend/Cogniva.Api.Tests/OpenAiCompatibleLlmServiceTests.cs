using System.Net;
using System.Text;
using System.Text.Json;
using Cogniva.Api.Configuration;
using Cogniva.Api.ExternalServices.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Tests;

public sealed class OpenAiCompatibleLlmServiceTests
{
    [Fact]
    public async Task GenerateResponseAsync_UsesOpenAiChatFormat_AndParsesResponse()
    {
        string? requestJson = null;
        string? authorization = null;
        var handler = new DelegateHandler(async request =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();
            authorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"  Odgovor na srpskom.  \"}}]}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/v1/") };
        var options = Options.Create(new AiOptions
        {
            BaseUrl = "http://localhost/v1/",
            ApiKey = "test-key",
            ChatModel = "test-chat-model"
        });
        var service = new OpenAiCompatibleLlmService(client, options, NullLogger<OpenAiCompatibleLlmService>.Instance);

        var answer = await service.GenerateResponseAsync([
            new LlmChatMessage("system", "Instrukcije"),
            new LlmChatMessage("user", "Pitanje")], maxTokens: 700, model: "summary-model");

        Assert.Equal("Odgovor na srpskom.", answer);
        Assert.Equal("Bearer test-key", authorization);
        using var json = JsonDocument.Parse(requestJson!);
        Assert.Equal("summary-model", json.RootElement.GetProperty("model").GetString());
        Assert.Equal("none", json.RootElement.GetProperty("reasoning_effort").GetString());
        Assert.Equal(700, json.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal("system", json.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("Pitanje", json.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task GenerateResponseAsync_DoesNotRequireApiKey_ForLocalCompatibleProvider()
    {
        string? requestJson = null;
        var handler = new DelegateHandler(async request =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Lokalni odgovor\"}}]}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var service = new OpenAiCompatibleLlmService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") },
            Options.Create(new AiOptions { ChatModel = "local-model", ApiKey = "" }),
            NullLogger<OpenAiCompatibleLlmService>.Instance);

        Assert.Equal("Lokalni odgovor", await service.GenerateResponseAsync([new("user", "Test")]));
        using var json = JsonDocument.Parse(requestJson!);
        Assert.Equal("local-model", json.RootElement.GetProperty("model").GetString());
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request);
    }
}
