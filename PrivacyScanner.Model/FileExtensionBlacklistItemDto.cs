namespace ITTitans.PrivacyScanner.Model;

/// <summary>A file extension excluded from scans.</summary>
public class FileExtensionBlacklistItemDto
{
    public required string Extension { get; init; }
}
