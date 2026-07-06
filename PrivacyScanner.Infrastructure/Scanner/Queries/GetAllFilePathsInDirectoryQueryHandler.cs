using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

/// <summary>
/// Enumerates all files under a root directory, excluding those matching the directory or file extension blacklist.
/// </summary>
public class GetAllFilePathsInDirectoryQueryHandler(IDirectoryProvider directoryProvider)
    : IRequestHandler<GetAllFilePathsInDirectoryQuery, GetAllFilePathsInDirectoryQueryResult>
{
    public ValueTask<GetAllFilePathsInDirectoryQueryResult> Handle(GetAllFilePathsInDirectoryQuery request, CancellationToken cancellationToken)
    {
        if (!directoryProvider.DirectoryExists(request.RootDirectory.FullName))
        {
            return new ValueTask<GetAllFilePathsInDirectoryQueryResult>(new GetAllFilePathsInDirectoryQueryResult
            {
                FileCount = 0,
                FilePaths = []
            });
        }

        var fileBlacklist = request.FileExtensionBlacklistItems.Select(i => i.Extension).ToArray();
        var directoryBlacklist = request.DirectoryBlacklistItems.Select(i => i.DirectoryName).ToArray();

        var allPaths = directoryProvider.GetFiles(request.RootDirectory.FullName, "*.*", SearchOption.AllDirectories);

        var files = allPaths.Where(p => !IsBlacklistedFilePath(p, directoryBlacklist, fileBlacklist))
            .Select(path => new FileInfo(path))
            .ToList();

        return new ValueTask<GetAllFilePathsInDirectoryQueryResult>(new GetAllFilePathsInDirectoryQueryResult
        {
            FileCount = files.Count,
            FilePaths = files
        });
    }

    private static bool IsBlacklistedFilePath(string path, string[] directoryBlacklist, string[] fileBlacklist)
    {
        foreach (var blacklistedDirectory in directoryBlacklist)
        {
            if (path.IndexOf($"{Path.DirectorySeparatorChar}{blacklistedDirectory}{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return true;
            }
        }

        foreach (var blacklistedFileExtension in fileBlacklist)
        {
            if (Path.GetExtension(path).Equals(blacklistedFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
