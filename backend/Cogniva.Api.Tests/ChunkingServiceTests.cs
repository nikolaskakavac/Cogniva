using Cogniva.Api.Configuration;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Tests;

public sealed class ChunkingServiceTests
{
    [Fact]
    public void CreateChunks_PreservesOrderOverlapAndPageMetadata()
    {
        var service = CreateService(target: 24, overlap: 8, minimum: 4);
        var document = new ExtractedDocument([
            new ExtractedSection("Prvi pasus sadrži dovoljno reči za početak dokumenta.\n\nDrugi pasus nastavlja sadržaj prve strane.", 1),
            new ExtractedSection("Treći pasus pripada drugoj strani i završava dokument.\n\nČetvrti pasus dodaje još teksta za novi deo.", 2)
        ]);

        var chunks = service.CreateChunks(document);

        Assert.True(chunks.Count >= 2);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.ChunkIndex));
        Assert.Equal(1, chunks[0].PageNumber);
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Content)));
        var firstWords = chunks[0].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var secondWords = chunks[1].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        Assert.NotEmpty(firstWords.Intersect(secondWords));
    }

    [Fact]
    public void CreateChunks_SplitsOneVeryLongParagraphDeterministically()
    {
        var service = CreateService(target: 20, overlap: 4, minimum: 3);
        var longParagraph = string.Join(' ', Enumerable.Range(1, 180).Select(index => $"reč{index}"));

        var first = service.CreateChunks(new ExtractedDocument([new ExtractedSection(longParagraph, null)]));
        var second = service.CreateChunks(new ExtractedDocument([new ExtractedSection(longParagraph, null)]));

        Assert.True(first.Count > 1);
        Assert.Equal(first, second);
        Assert.All(first, chunk => Assert.Null(chunk.PageNumber));
    }

    private static ChunkingService CreateService(int target, int overlap, int minimum) =>
        new(Options.Create(new ChunkingOptions
        {
            TargetTokens = target,
            OverlapTokens = overlap,
            MinimumTokens = minimum
        }));
}
