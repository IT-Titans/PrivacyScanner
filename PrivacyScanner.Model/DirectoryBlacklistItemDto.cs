namespace ITTitans.PrivacyScanner.Model;

/// <summary>A directory name excluded from scans wherever it occurs in a scanned path.</summary>
public class DirectoryBlacklistItemDto
{
    public required string DirectoryName { get; init; }
}
