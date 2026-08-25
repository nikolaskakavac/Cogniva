using System.Text;
using Cogniva.Api.Configuration;
using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Middleware;
using Cogniva.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Services;

public sealed class SummaryService(
    ILlmService llmService,
    IOptions<SummaryOptions> options,
    IOptions<AiOptions> aiOptions) : ISummaryService
{
    private const string PartialSystemPrompt =
        "/no_think\nIzdvoj najvažnije činjenice i teme. Ne izmišljaj. Piši na srpskom, latinicom.";

    private const string FinalSystemPrompt =
        "/no_think\nNapravi pregledan sažetak na srpskom, latinicom. Ne izmišljaj i vrati samo sažetak.";

    private readonly SummaryOptions _options = options.Value;
    private readonly string _summaryModel = string.IsNullOrWhiteSpace(aiOptions.Value.SummaryModel)
        ? aiOptions.Value.ChatModel
        : aiOptions.Value.SummaryModel.Trim();

    public async Task<string> GenerateAsync(
        string documentName,
        IReadOnlyList<string> chunks,
        CancellationToken cancellationToken = default)
    {
        var usefulChunks = chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .Select(chunk => chunk.Trim())
            .ToList();
        var totalCharacters = usefulChunks.Sum(chunk => chunk.Length) + Math.Max(0, usefulChunks.Count - 1) * 2;

        if (usefulChunks.Count == 0 || totalCharacters < 20)
        {
            throw new ApiException(422, "Dokument nema dovoljno teksta.",
                "Nije moguće generisati sažetak jer dokument nema dovoljno teksta.");
        }

        if (totalCharacters <= _options.DirectSummaryMaxCharacters)
        {
            return await GenerateFinalAsync(
                documentName,
                string.Join("\n\n", usefulChunks),
                "Sažmi sledeći dokument. Izdvoji kratak pregled i ključne tačke.",
                cancellationToken);
        }

        var batches = BuildBatches(usefulChunks, _options.PartialBatchMaxCharacters);
        var partialSummaries = new List<string>(batches.Count);
        foreach (var batch in batches)
        {
            partialSummaries.Add(await llmService.GenerateResponseAsync([
                new LlmChatMessage("system", PartialSystemPrompt),
                new LlmChatMessage("user", $"Deo dokumenta \"{documentName}\":\n\n{batch}")
            ], cancellationToken, _options.PartialMaxTokens, _summaryModel));
        }

        while (partialSummaries.Count > _options.MaxPartialSummariesPerFinalPrompt)
        {
            var reduced = new List<string>();
            foreach (var group in partialSummaries.Chunk(_options.MaxPartialSummariesPerFinalPrompt))
            {
                reduced.Add(await llmService.GenerateResponseAsync([
                    new LlmChatMessage("system", PartialSystemPrompt),
                    new LlmChatMessage("user", $"Spoji ove beleške bez ponavljanja:\n\n{string.Join("\n\n", group)}")
                ], cancellationToken, _options.PartialMaxTokens, _summaryModel));
            }
            partialSummaries = reduced;
        }

        return await GenerateFinalAsync(
            documentName,
            string.Join("\n\n", partialSummaries),
            "Spoji parcijalne sažetke, ukloni ponavljanja i izdvoji kratak pregled i ključne tačke.",
            cancellationToken);
    }

    private Task<string> GenerateFinalAsync(
        string documentName,
        string content,
        string instruction,
        CancellationToken cancellationToken) =>
        llmService.GenerateResponseAsync([
            new LlmChatMessage("system", FinalSystemPrompt),
            new LlmChatMessage("user", $"{instruction}\nDokument: \"{documentName}\"\n\n{content}")
        ], cancellationToken, _options.FinalMaxTokens, _summaryModel);

    internal static IReadOnlyList<string> BuildBatches(IReadOnlyList<string> chunks, int limit)
    {
        var batches = new List<string>();
        var current = new StringBuilder();
        foreach (var chunk in chunks)
        {
            if (current.Length > 0 && current.Length + chunk.Length + 2 > limit)
            {
                batches.Add(current.ToString());
                current.Clear();
            }

            if (chunk.Length <= limit)
            {
                if (current.Length > 0) current.AppendLine().AppendLine();
                current.Append(chunk);
                continue;
            }

            if (current.Length > 0) { batches.Add(current.ToString()); current.Clear(); }
            for (var offset = 0; offset < chunk.Length; offset += limit)
            {
                batches.Add(chunk.Substring(offset, Math.Min(limit, chunk.Length - offset)));
            }
        }
        if (current.Length > 0) batches.Add(current.ToString());
        return batches;
    }
}
