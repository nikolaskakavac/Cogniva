using System.Text.RegularExpressions;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Cogniva.Api.Services;

public sealed partial class TextExtractionService : ITextExtractionService
{
    public Task<ExtractedDocument> ExtractAsync(
        string physicalPath,
        string fileType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return fileType.ToUpperInvariant() switch
        {
            "PDF" => Task.FromResult(ExtractPdf(physicalPath, cancellationToken)),
            "DOCX" => Task.FromResult(ExtractDocx(physicalPath, cancellationToken)),
            _ => throw new DocumentProcessingException("Format dokumenta nije podržan za obradu.")
        };
    }

    private static ExtractedDocument ExtractPdf(string physicalPath, CancellationToken cancellationToken)
    {
        var sections = new List<ExtractedSection>();
        using var pdf = PdfDocument.Open(physicalPath);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = NormalizeText(ContentOrderTextExtractor.GetText(page));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sections.Add(new ExtractedSection(text, page.Number));
            }
        }

        if (sections.Sum(section => section.Text.Length) < 20)
        {
            throw new DocumentProcessingException(
                "Nije moguće izdvojiti tekst iz dokumenta. Skenirani PDF dokumenti trenutno nisu podržani.");
        }

        return new ExtractedDocument(sections);
    }

    private static ExtractedDocument ExtractDocx(string physicalPath, CancellationToken cancellationToken)
    {
        using var wordDocument = WordprocessingDocument.Open(physicalPath, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;
        var blocks = new List<string>();

        if (body is not null)
        {
            foreach (var child in body.ChildElements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (child)
                {
                    case Paragraph paragraph:
                        AddBlock(blocks, paragraph.InnerText);
                        break;
                    case Table table:
                        foreach (var row in table.Elements<TableRow>())
                        {
                            var cells = row.Elements<TableCell>()
                                .Select(cell => NormalizeInlineText(cell.InnerText))
                                .Where(text => text.Length > 0);
                            AddBlock(blocks, string.Join(" | ", cells));
                        }
                        break;
                }
            }
        }

        var text = NormalizeText(string.Join("\n\n", blocks));
        if (text.Length < 20)
        {
            throw new DocumentProcessingException("Nije moguće izdvojiti koristan tekst iz DOCX dokumenta.");
        }

        return new ExtractedDocument([new ExtractedSection(text, null)]);
    }

    private static void AddBlock(ICollection<string> blocks, string value)
    {
        var normalized = NormalizeInlineText(value);
        if (normalized.Length > 0) blocks.Add(normalized);
    }

    private static string NormalizeInlineText(string value) => InlineWhitespaceRegex().Replace(value, " ").Trim();

    private static string NormalizeText(string value)
    {
        var normalizedBreaks = value.Replace("\r\n", "\n").Replace('\r', '\n');
        normalizedBreaks = InlineWhitespaceRegex().Replace(normalizedBreaks, " ");
        normalizedBreaks = AroundNewlineWhitespaceRegex().Replace(normalizedBreaks, "\n");
        return ExcessiveBreaksRegex().Replace(normalizedBreaks, "\n\n").Trim();
    }

    [GeneratedRegex(@"[\t\f\v ]+")]
    private static partial Regex InlineWhitespaceRegex();

    [GeneratedRegex(@" *\n *")]
    private static partial Regex AroundNewlineWhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveBreaksRegex();
}
