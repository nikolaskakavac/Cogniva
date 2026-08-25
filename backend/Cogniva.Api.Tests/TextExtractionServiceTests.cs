using System.Text;
using Cogniva.Api.Models.Processing;
using Cogniva.Api.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Cogniva.Api.Tests;

public sealed class TextExtractionServiceTests
{
    private readonly TextExtractionService _service = new();

    [Fact]
    public async Task ExtractAsync_ReadsDocxParagraphsAndTableWithoutPageNumbers()
    {
        var path = CreateDocx(includeText: true);
        try
        {
            var result = await _service.ExtractAsync(path, "DOCX");
            var section = Assert.Single(result.Sections);
            Assert.Null(section.PageNumber);
            Assert.Contains("Naslov dokumenta", section.Text);
            Assert.Contains("Prvi pasus", section.Text);
            Assert.Contains("Ćelija jedan | Ćelija dva", section.Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ExtractAsync_RejectsEmptyDocx()
    {
        var path = CreateDocx(includeText: false);
        try
        {
            var exception = await Assert.ThrowsAsync<DocumentProcessingException>(
                () => _service.ExtractAsync(path, "DOCX"));
            Assert.Contains("koristan tekst", exception.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ExtractAsync_ReadsMultiplePdfPagesWithCorrectNumbers()
    {
        var path = CreatePdf(["Prva stranica sa tekstualnim slojem.", "Druga stranica sa dodatnim tekstom."]);
        try
        {
            var result = await _service.ExtractAsync(path, "PDF");
            Assert.Equal([1, 2], result.Sections.Select(section => section.PageNumber));
            Assert.Contains("Prva stranica", result.Sections[0].Text);
            Assert.Contains("Druga stranica", result.Sections[1].Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ExtractAsync_RejectsPdfWithoutUsefulTextLayer()
    {
        var path = CreatePdf([""]);
        try
        {
            var exception = await Assert.ThrowsAsync<DocumentProcessingException>(
                () => _service.ExtractAsync(path, "PDF"));
            Assert.Contains("Skenirani PDF", exception.Message);
        }
        finally { File.Delete(path); }
    }

    private static string CreateDocx(bool includeText)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        using var document = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;
        if (includeText)
        {
            body.Append(new Paragraph(new Run(new Text("Naslov dokumenta"))));
            body.Append(new Paragraph(new Run(new Text("Prvi pasus sa sadržajem za proveru ekstrakcije."))));
            body.Append(new Table(new TableRow(
                new TableCell(new Paragraph(new Run(new Text("Ćelija jedan")))),
                new TableCell(new Paragraph(new Run(new Text("Ćelija dva")))))));
        }
        mainPart.Document.Save();
        return path;
    }

    private static string CreatePdf(IReadOnlyList<string> pageTexts)
    {
        var objects = new List<string>();
        var pageIds = pageTexts.Select((_, index) => 3 + index * 2).ToArray();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(id => $"{id} 0 R"))}] /Count {pageTexts.Count} >>");
        for (var index = 0; index < pageTexts.Count; index++)
        {
            var pageId = pageIds[index];
            var contentId = pageId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {pageTexts.Count * 2 + 3} 0 R >> >> /Contents {contentId} 0 R >>");
            var escaped = pageTexts[index].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            var content = string.IsNullOrEmpty(escaped) ? "" : $"BT /F1 12 Tf 50 740 Td ({escaped}) Tj ET";
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
        }
        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefOffset).Append("\n%%EOF");
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, builder.ToString(), Encoding.ASCII);
        return path;
    }
}
