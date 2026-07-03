using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Thin wrapper around <see cref="System.IO"/> file system access, allowing it to be mocked in tests.
/// </summary>
public class FileSystemService : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(path, searchPattern, searchOption);
}
