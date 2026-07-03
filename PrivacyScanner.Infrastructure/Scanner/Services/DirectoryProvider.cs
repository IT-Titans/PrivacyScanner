using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Default <see cref="IDirectoryProvider"/> implementation, backed directly by <see cref="Directory"/>.
/// </summary>
public class DirectoryProvider : IDirectoryProvider
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(path, searchPattern, searchOption);
}
