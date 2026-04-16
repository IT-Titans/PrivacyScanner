using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

public static class DefaultBlacklistItemsProvider
{
    public static List<DirectoryBlacklistItemDto> GetDefaultDirectoryItems()
    {
        return new[]
        {
            "bin",
            "obj",
            ".git",
            "node_modules",
            ".nuxt",
            ".idea",
            ".vs"
        }.Select(i => new DirectoryBlacklistItemDto()
        {
            DirectoryName = i,
        })
        .ToList();
    }

    public static List<FileExtensionBlacklistItemDto> GetDefaultFileExtensionItems()
    {
        return new[]
            {
                ".dll",
                ".exe",
                ".g.cs",
                ".svg"
            }.Select(i => new FileExtensionBlacklistItemDto()
            {
                Extension = i,
            })
            .ToList();
    }
}