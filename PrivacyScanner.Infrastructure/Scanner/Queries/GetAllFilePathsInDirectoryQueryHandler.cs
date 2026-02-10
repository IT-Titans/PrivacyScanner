using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

public class GetAllFilePathsInDirectoryQueryHandler(IFileSystem fileSystem)
    : IRequestHandler<GetAllFilePathsInDirectoryQuery, GetAllFilePathsInDirectoryQueryResult>
{
    public ValueTask<GetAllFilePathsInDirectoryQueryResult> Handle(GetAllFilePathsInDirectoryQuery request, CancellationToken cancellationToken)
    {
        if (!fileSystem.DirectoryExists(request.RootDirectory.FullName))
        {
            return new ValueTask<GetAllFilePathsInDirectoryQueryResult>(new GetAllFilePathsInDirectoryQueryResult
            {
                FileCount = 0,
                FilePaths = []
            });
        }

        var fileBlacklist = new string[]
        {
            ".dll",
            ".exe",
            ".g.cs"
        };

        var directoryBlacklist = new string[]
        {
            "bin",
            "obj",
            ".git",
            "node_modules",
            ".nuxt",
            ".idea"
        };

        var allPaths = fileSystem.GetFiles(request.RootDirectory.FullName, "*.*", SearchOption.AllDirectories);

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

    public static bool IsBinaryFile(string filePath)
    {
        const int sampleSize = 8000;

        byte[] buffer = new byte[sampleSize];

        using var stream = File.OpenRead(filePath);
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == 0)
            {

                return true;
            }
        }

        return false;
    }

}