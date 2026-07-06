namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

/// <summary>Abstracts directory traversal used by the scanner so it can be mocked in tests.</summary>
public interface IDirectoryProvider
{
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
}
