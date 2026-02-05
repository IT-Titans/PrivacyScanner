using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

public class FileSystemService : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(path, searchPattern, searchOption);

    public string ReadAllText(string path) => File.ReadAllText(path);
}
