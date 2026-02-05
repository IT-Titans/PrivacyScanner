using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllFilePathsInDirectoryQuery : IRequest<GetAllFilePathsInDirectoryQueryResult>
{
    public required DirectoryInfo RootDirectory { get; init; }
}

public class GetAllFilePathsInDirectoryQueryResult
{
    public required IEnumerable<FileInfo> FilePaths { get; init; }
    public required int FileCount { get; init; }
}