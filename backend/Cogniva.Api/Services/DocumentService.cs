using Cogniva.Api.Configuration;
using Cogniva.Api.Data;
using Cogniva.Api.DTOs.Documents;
using Cogniva.Api.Middleware;
using Cogniva.Api.Models;
using Cogniva.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cogniva.Api.Services;

public sealed class DocumentService(
    AppDbContext dbContext,
    IWebHostEnvironment environment,
    IOptions<FileStorageOptions> storageOptions,
    ISummaryService summaryService,
    ILogger<DocumentService> logger) : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };

    private static readonly Dictionary<string, HashSet<string>> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "application/octet-stream"
            },
            [".docx"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/octet-stream"
            }
        };

    private readonly FileStorageOptions _storageOptions = storageOptions.Value;

    public async Task<IReadOnlyList<DocumentListItemResponse>> GetDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(document => document.UserId == userId)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new DocumentListItemResponse(
                document.Id,
                document.Name,
                document.OriginalFileName,
                document.FileType,
                document.Status.ToString(),
                document.UploadedAt,
                document.ProcessedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentDetailsResponse> GetDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Id == documentId)
            .Select(item => new DocumentDetailsResponse(
                item.Id,
                item.Name,
                item.OriginalFileName,
                item.FileType,
                item.Status.ToString(),
                item.Summary,
                item.ProcessingError,
                item.UploadedAt,
                item.ProcessedAt,
                item.Chunks.Count))
            .SingleOrDefaultAsync(cancellationToken);

        return document ?? throw DocumentNotFound();
    }

    public async Task<UploadDocumentResponse> UploadDocumentAsync(
        Guid userId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var extension = ValidateFile(file);
        var originalFileName = Path.GetFileName(file!.FileName);

        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Length > 255)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Neispravan naziv dokumenta.",
                "Naziv dokumenta nije ispravan ili je predugačak.");
        }

        var serverFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);
        var physicalPath = ResolveStoragePath(uploadRoot, serverFileName);

        try
        {
            await using var output = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);
            await file.CopyToAsync(output, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Failed to store uploaded document {ServerFileName}", serverFileName);
            throw new ApiException(
                StatusCodes.Status500InternalServerError,
                "Dokument nije sačuvan.",
                "Došlo je do greške prilikom čuvanja dokumenta.");
        }

        var document = new Document
        {
            UserId = userId,
            Name = Path.GetFileNameWithoutExtension(originalFileName),
            OriginalFileName = originalFileName,
            FileType = extension.TrimStart('.').ToUpperInvariant(),
            FilePath = serverFileName,
            Status = DocumentStatus.Uploaded
        };

        dbContext.Documents.Add(document);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDeletePhysicalFile(physicalPath, document.Id);
            throw;
        }

        return new UploadDocumentResponse(
            document.Id,
            document.Name,
            document.OriginalFileName,
            document.FileType,
            document.Status.ToString(),
            document.UploadedAt);
    }

    public async Task DeleteDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Id == documentId,
            cancellationToken) ?? throw DocumentNotFound();

        var physicalPath = ResolveStoragePath(GetUploadRoot(), document.FilePath);
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        TryDeletePhysicalFile(physicalPath, document.Id);
    }

    public async Task<DocumentDetailsResponse> SummarizeDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .Include(item => item.Chunks.OrderBy(chunk => chunk.ChunkIndex))
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Id == documentId, cancellationToken)
            ?? throw DocumentNotFound();

        if (document.Status != DocumentStatus.Ready)
        {
            throw new ApiException(409, "Dokument nije spreman.", "Dokument mora biti obrađen pre generisanja sažetka.");
        }

        var summary = await summaryService.GenerateAsync(
            document.OriginalFileName,
            document.Chunks.Select(chunk => chunk.Content).ToList(),
            cancellationToken);
        document.Summary = summary;
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return await GetDocumentAsync(userId, documentId, CancellationToken.None);
    }

    private string ValidateFile(IFormFile? file)
    {
        if (file is null)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Fajl nije izabran.",
                "Izaberite PDF ili DOCX dokument.");
        }

        if (file.Length == 0)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Dokument je prazan.",
                "Izabrani dokument ne sadrži podatke.");
        }

        var maximumBytes = _storageOptions.MaxFileSizeMb * 1024L * 1024L;
        if (file.Length > maximumBytes)
        {
            throw new ApiException(
                StatusCodes.Status413PayloadTooLarge,
                "Dokument je prevelik.",
                $"Dokument ne može biti veći od {_storageOptions.MaxFileSizeMb} MB.");
        }

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Format dokumenta nije podržan.",
                "Podržani su samo PDF i DOCX dokumenti.");
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !AllowedContentTypes[extension].Contains(file.ContentType))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Tip dokumenta nije podržan.",
                "Sadržaj fajla ne odgovara podržanom PDF ili DOCX dokumentu.");
        }

        return extension;
    }

    private string GetUploadRoot()
    {
        var configuredPath = _storageOptions.UploadPath;
        return Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    private static string ResolveStoragePath(string uploadRoot, string storedFileName)
    {
        var rootWithSeparator = Path.TrimEndingDirectorySeparator(uploadRoot) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(uploadRoot, storedFileName));

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stored document path is outside the configured upload directory.");
        }

        return candidate;
    }

    private void TryDeletePhysicalFile(string physicalPath, Guid documentId)
    {
        try
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Failed to delete physical file for document {DocumentId}", documentId);
        }
    }

    private static ApiException DocumentNotFound() => new(
        StatusCodes.Status404NotFound,
        "Dokument nije pronađen.",
        "Traženi dokument ne postoji.");
}
