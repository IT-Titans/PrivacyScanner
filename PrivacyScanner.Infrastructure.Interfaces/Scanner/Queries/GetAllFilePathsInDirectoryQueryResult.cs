namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllFilePathsInDirectoryQueryResult
{
    public required IEnumerable<FileInfo> FilePaths { get; init; }
    public required int FileCount { get; init; }
}