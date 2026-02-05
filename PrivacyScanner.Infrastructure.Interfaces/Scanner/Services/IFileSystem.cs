namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
    string ReadAllText(string path);
}
