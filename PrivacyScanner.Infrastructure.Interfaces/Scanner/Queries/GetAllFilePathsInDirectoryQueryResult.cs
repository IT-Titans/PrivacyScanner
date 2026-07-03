namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

/// <summary>The file paths found under a scanned root directory, and their total count.</summary>
public class GetAllFilePathsInDirectoryQueryResult
{
    public required IEnumerable<FileInfo> FilePaths { get; init; }
    public required int FileCount { get; init; }
}
