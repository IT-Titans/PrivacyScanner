using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>Verifies the built-in blacklist entries a fresh installation starts with.</summary>
public class DefaultBlacklistItemsProviderI3Tests
{
    [Fact]
    public void DefaultDirectories_AreTheBuildDependencyAndToolFolders()
    {
        var names = DefaultBlacklistItemsProvider.GetDefaultDirectoryItems().Select(i => i.DirectoryName);

        Assert.Equal(["bin", "obj", ".git", "node_modules", ".nuxt", ".idea", ".vs"], names);
    }

    [Fact]
    public void DefaultExtensions_AreBinaryAndGeneratedFileTypes()
    {
        var extensions = DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems().Select(i => i.Extension);

        Assert.Equal([".dll", ".exe", ".g.cs", ".svg"], extensions);
    }

    [Fact]
    public void DefaultEntries_AreFreshCopiesSoEditingOneResultDoesNotChangeTheNext()
    {
        DefaultBlacklistItemsProvider.GetDefaultDirectoryItems().Clear();
        DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems().Clear();

        Assert.NotEmpty(DefaultBlacklistItemsProvider.GetDefaultDirectoryItems());
        Assert.NotEmpty(DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems());
    }

    [Fact]
    public void DefaultEntries_HaveNoDuplicatesAndExtensionsStartWithADot()
    {
        var directories = DefaultBlacklistItemsProvider.GetDefaultDirectoryItems().Select(i => i.DirectoryName).ToList();
        var extensions = DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems().Select(i => i.Extension).ToList();

        Assert.Equal(directories.Count, directories.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(extensions.Count, extensions.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(extensions, e => Assert.StartsWith(".", e));
    }

    [Fact]
    public async Task DefaultExtensionGCs_ExcludesGeneratedSourceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var generated = Path.Combine(root, "Foo.g.cs");
        var source = Path.Combine(root, "Foo.cs");
        var directoryProvider = new Mock<IDirectoryProvider>();
        directoryProvider.Setup(d => d.DirectoryExists(It.IsAny<string>())).Returns(true);
        directoryProvider.Setup(d => d.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories)).Returns([generated, source]);

        var result = await new GetAllFilePathsInDirectoryQueryHandler(directoryProvider.Object).Handle(
            new GetAllFilePathsInDirectoryQuery
            {
                RootDirectory = new DirectoryInfo(root),
                DirectoryBlacklistItems = [],
                FileExtensionBlacklistItems = DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems(),
            },
            CancellationToken.None);

        Assert.Equal("Foo.cs", Assert.Single(result.FilePaths).Name);
    }
}
