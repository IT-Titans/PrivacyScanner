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

        var fileName = Path.GetFileName(path.AsSpan());

        foreach (var blacklistedFileExtension in fileBlacklist)
        {
            if (EndsWithExtension(fileName, blacklistedFileExtension))
            {
                return true;
            }
        }

        return false;
    }

    // An entry is a dotted suffix of the file name (".cs", ".g.cs"). Path.GetExtension only yields the last part,
    // so multi-part entries could never match. Entries without a leading dot or consisting of the dot alone never
    // matched before and still do not. A file named exactly like the entry (".gitignore") matches, as before.
    private static bool EndsWithExtension(ReadOnlySpan<char> fileName, string extension)
    {
        return extension.Length > 1
            && extension[0] == '.'
            && fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }
}
