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

        // var files = fileSystem.GetFiles(request.RootDirectory.FullName, "*.*", SearchOption.AllDirectories)
        //     .Where(file => !new[] { ".exe", ".docx", ".pdf", ".jpg" }.Any(endungen => file.Contains(endungen)))
        //     .Select(path => new FileInfo(path))
        //     .ToList();

        var files = fileSystem.GetFiles(request.RootDirectory.FullName, "*.*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .ToList();

        return new ValueTask<GetAllFilePathsInDirectoryQueryResult>(new GetAllFilePathsInDirectoryQueryResult
        {
            FileCount = files.Count,
            FilePaths = files
        });
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