using System.Net;
using System.Text;
using Cogniva.Api.Configuration;
using Cogniva.Api.ExternalServices.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Tests;

public sealed class OpenAiEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingsAsync_UsesBatchAndPreservesProviderOrder()
    {
        var handler = new FakeEmbeddingHandler();
        var service = new OpenAiEmbeddingService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/v1/") },
            Options.Create(new AiOptions
            {
                ApiKey = "test-only-key",
                EmbeddingModel = "test-model",
                EmbeddingDimensions = 3
            }),
            NullLogger<OpenAiEmbeddingService>.Instance);

        var embeddings = await service.GenerateEmbeddingsAsync(["prvi", "drugi"]);

        Assert.Equal(2, embeddings.Count);
        Assert.Equal([1f, 2f, 3f], embeddings[0]);
        Assert.Equal([4f, 5f, 6f], embeddings[1]);
        Assert.Contains("\"input\":[\"prvi\",\"drugi\"]", handler.RequestBody);
    }

    private sealed class FakeEmbeddingHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"data\":[{\"index\":0,\"embedding\":[1,2,3]},{\"index\":1,\"embedding\":[4,5,6]}]}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
