using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Default <see cref="IDirectoryProvider"/> implementation, backed directly by <see cref="Directory"/>.
/// </summary>
public class DirectoryProvider(ILogger<DirectoryProvider> logger) : IDirectoryProvider
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    /// Recursively collects files under <paramref name="path"/>, skipping (and logging) subdirectories
    /// that cannot be accessed instead of aborting the whole enumeration — unlike
    /// <see cref="Directory.GetFiles(string, string, SearchOption)"/>, which throws and returns nothing
    /// at all if a single subdirectory anywhere in the tree is access-restricted.
    /// </summary>
    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        var files = new List<string>();
        CollectFiles(path, searchPattern, searchOption, files);
        return files;
    }

    private void CollectFiles(string directory, string searchPattern, SearchOption searchOption, List<string> files)
    {
        try
        {
            files.AddRange(Directory.GetFiles(directory, searchPattern));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogWarning(ex, "Verzeichnis {Directory} konnte nicht gelesen werden und wird übersprungen", directory);
            return;
        }

        if (searchOption != SearchOption.AllDirectories)
            return;

        string[] subDirectories;
        try
        {
            subDirectories = Directory.GetDirectories(directory);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogWarning(ex, "Unterverzeichnisse von {Directory} konnten nicht aufgelistet werden und werden übersprungen", directory);
            return;
        }

        foreach (var subDirectory in subDirectories)
        {
            CollectFiles(subDirectory, searchPattern, searchOption, files);
        }
    }
}
