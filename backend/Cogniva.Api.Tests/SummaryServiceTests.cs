using Cogniva.Api.Configuration;
using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Middleware;
using Cogniva.Api.Services;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Tests;

public sealed class SummaryServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShortDocument_UsesExactlyOneFinalCall()
    {
        var llm = new RecordingLlmService(new[] { "Konačan sažetak" });
        var service = CreateService(llm);

        var result = await service.GenerateAsync("kratak.pdf", ["Dovoljno dug sadržaj kratkog dokumenta."]);

        Assert.Equal("Konačan sažetak", result);
        Assert.Single(llm.Calls);
        Assert.Equal(700, llm.Calls[0].MaxTokens);
        Assert.Equal("summary-model", llm.Calls[0].Model);
        Assert.Contains("Sažmi sledeći dokument", llm.Calls[0].Messages[1].Content);
    }

    [Fact]
    public async Task GenerateAsync_MediumDocumentWithinDirectLimit_UsesExactlyOneCall()
    {
        var llm = new RecordingLlmService(new[] { "Direktan sažetak" });
        var service = CreateService(llm, configureSummaryModel: false);
        var chunks = Enumerable.Range(0, 4).Select(_ => new string('A', 3000)).ToArray();

        await service.GenerateAsync("srednji.docx", chunks);

        Assert.Single(llm.Calls);
        Assert.Equal(700, llm.Calls[0].MaxTokens);
        Assert.Equal("chat-model", llm.Calls[0].Model);
    }

    [Fact]
    public async Task GenerateAsync_LongDocument_BatchesChunksAndAddsOneFinalCall()
    {
        var llm = new RecordingLlmService(new[] { "P1", "P2", "P3", "P4", "Konačan sažetak" });
        var service = CreateService(llm);
        var chunks = Enumerable.Range(0, 20).Select(_ => new string('A', 1800)).ToArray();

        var result = await service.GenerateAsync("dug.docx", chunks);

        Assert.Equal("Konačan sažetak", result);
        Assert.Equal(5, llm.Calls.Count);
        Assert.All(llm.Calls.Take(4), call => Assert.Equal(350, call.MaxTokens));
        Assert.Equal(700, llm.Calls[^1].MaxTokens);
        Assert.All(llm.Calls, call => Assert.Equal("summary-model", call.Model));
        Assert.Contains("Spoji parcijalne sažetke", llm.Calls[^1].Messages[1].Content);
    }

    [Fact]
    public async Task GenerateAsync_RejectsEmptyContentWithoutCallingLlm()
    {
        var llm = new RecordingLlmService(Array.Empty<string>());
        var service = CreateService(llm);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GenerateAsync("prazan.pdf", ["  "]));

        Assert.Equal(422, exception.StatusCode);
        Assert.Empty(llm.Calls);
    }

    [Fact]
    public async Task GenerateAsync_LlmFailure_DoesNotReturnFakeSummary()
    {
        var llm = new FailingLlmService();
        var service = CreateService(llm);

        await Assert.ThrowsAsync<ApiException>(() =>
            service.GenerateAsync("test.pdf", ["Dovoljno dug sadržaj dokumenta za test greške servisa."]));
        Assert.Equal(1, llm.CallCount);
    }

    private static SummaryService CreateService(ILlmService llm, bool configureSummaryModel = true) => new(
        llm,
        Options.Create(new SummaryOptions
        {
            DirectSummaryMaxCharacters = 16000,
            PartialBatchMaxCharacters = 12000,
            MaxPartialSummariesPerFinalPrompt = 8,
            PartialMaxTokens = 350,
            FinalMaxTokens = 700
        }),
        Options.Create(new AiOptions
        {
            ChatModel = "chat-model",
            SummaryModel = configureSummaryModel ? "summary-model" : "  "
        }));

    private sealed record RecordedCall(IReadOnlyList<LlmChatMessage> Messages, int MaxTokens, string? Model);

    private sealed class RecordingLlmService(Queue<string> responses) : ILlmService
    {
        public RecordingLlmService(IEnumerable<string> responses) : this(new Queue<string>(responses)) { }
        public List<RecordedCall> Calls { get; } = [];

        public Task<string> GenerateResponseAsync(
            IReadOnlyList<LlmChatMessage> messages,
            CancellationToken cancellationToken = default,
            int maxTokens = 1024,
            string? model = null)
        {
            Calls.Add(new RecordedCall(messages, maxTokens, model));
            return Task.FromResult(responses.Dequeue());
        }
    }

    private sealed class FailingLlmService : ILlmService
    {
        public int CallCount { get; private set; }

        public Task<string> GenerateResponseAsync(
            IReadOnlyList<LlmChatMessage> messages,
            CancellationToken cancellationToken = default,
            int maxTokens = 1024,
            string? model = null)
        {
            CallCount++;
            throw new ApiException(503, "AI servis trenutno nije dostupan.", "Pokušajte ponovo.");
        }
    }
}
