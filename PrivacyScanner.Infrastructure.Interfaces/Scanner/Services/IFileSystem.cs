namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

/// <summary>Abstracts file-system access used by the scanner so it can be mocked in tests.</summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
}
