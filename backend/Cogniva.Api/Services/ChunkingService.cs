using System.Text.RegularExpressions;
using Cogniva.Api.Configuration;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Services;

public sealed partial class ChunkingService(IOptions<ChunkingOptions> options) : IChunkingService
{
    private readonly ChunkingOptions _options = options.Value;

    public IReadOnlyList<TextChunk> CreateChunks(ExtractedDocument document)
    {
        var units = document.Sections
            .SelectMany(section => SplitIntoUnits(section.Text)
                .Select(text => new Unit(text, section.PageNumber)))
            .Where(unit => unit.Text.Length > 0)
            .ToList();

        if (units.Count == 0) return [];

        var chunks = new List<TextChunk>();
        var current = new List<Unit>();
        var currentTokens = 0;

        foreach (var unit in units)
        {
            var unitTokens = EstimateTokens(unit.Text);
            if (current.Count > 0 && currentTokens + unitTokens > _options.TargetTokens)
            {
                AddChunk(chunks, current);
                current = BuildOverlap(current);
                currentTokens = current.Sum(item => EstimateTokens(item.Text));
            }

            current.Add(unit);
            currentTokens += unitTokens;
        }

        if (current.Count > 0)
        {
            if (chunks.Count > 0 && currentTokens < _options.MinimumTokens)
            {
                var previous = chunks[^1];
                chunks[^1] = previous with { Content = $"{previous.Content}\n\n{Join(current)}" };
            }
            else
            {
                AddChunk(chunks, current);
            }
        }

        return chunks.Select((chunk, index) => chunk with { ChunkIndex = index }).ToList();
    }

    private IEnumerable<string> SplitIntoUnits(string text)
    {
        foreach (var paragraph in ParagraphBreakRegex().Split(text).Select(value => value.Trim()).Where(value => value.Length > 0))
        {
            if (EstimateTokens(paragraph) <= _options.TargetTokens)
            {
                yield return paragraph;
                continue;
            }

            var sentences = SentenceBreakRegex().Split(paragraph).Where(value => value.Length > 0);
            var buffer = new List<string>();
            var tokens = 0;
            foreach (var sentence in sentences)
            {
                foreach (var piece in SplitOversizedSentence(sentence))
                {
                    var pieceTokens = EstimateTokens(piece);
                    if (buffer.Count > 0 && tokens + pieceTokens > _options.TargetTokens)
                    {
                        yield return string.Join(" ", buffer);
                        buffer.Clear();
                        tokens = 0;
                    }
                    buffer.Add(piece);
                    tokens += pieceTokens;
                }
            }
            if (buffer.Count > 0) yield return string.Join(" ", buffer);
        }
    }

    private IEnumerable<string> SplitOversizedSentence(string sentence)
    {
        if (EstimateTokens(sentence) <= _options.TargetTokens)
        {
            yield return sentence.Trim();
            yield break;
        }

        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var approximateWordsPerChunk = Math.Max(1, _options.TargetTokens * 3 / 4);
        for (var index = 0; index < words.Length; index += approximateWordsPerChunk)
        {
            yield return string.Join(' ', words.Skip(index).Take(approximateWordsPerChunk));
        }
    }

    private List<Unit> BuildOverlap(IReadOnlyList<Unit> current)
    {
        var overlap = new List<Unit>();
        var tokens = 0;
        for (var index = current.Count - 1; index >= 0; index--)
        {
            var itemTokens = EstimateTokens(current[index].Text);
            if (overlap.Count > 0 && tokens + itemTokens > _options.OverlapTokens) break;
            overlap.Insert(0, current[index]);
            tokens += itemTokens;
        }
        return overlap;
    }

    private static int EstimateTokens(string text) => Math.Max(1, (int)Math.Ceiling(text.Length / 4d));

    private static string Join(IEnumerable<Unit> units) => string.Join("\n\n", units.Select(unit => unit.Text));

    private static void AddChunk(ICollection<TextChunk> chunks, IReadOnlyList<Unit> units)
    {
        chunks.Add(new TextChunk(Join(units), chunks.Count, units.Select(unit => unit.PageNumber).FirstOrDefault(page => page.HasValue)));
    }

    private sealed record Unit(string Text, int? PageNumber);

    [GeneratedRegex(@"\n\s*\n+")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBreakRegex();
}
