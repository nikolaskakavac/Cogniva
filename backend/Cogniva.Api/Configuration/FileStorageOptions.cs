using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string UploadPath { get; init; } = "Storage/uploads";

    [Range(1, 100)]
    public int MaxFileSizeMb { get; init; } = 20;
}
