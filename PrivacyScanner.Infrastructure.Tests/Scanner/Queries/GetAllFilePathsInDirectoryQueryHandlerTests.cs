using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Queries;

/// <summary>Verifies directory-name and file-extension blacklist filtering when enumerating scan targets.</summary>
public class GetAllFilePathsInDirectoryQueryHandlerTests
{
    private readonly Mock<IDirectoryProvider> _directoryProviderMock = new();

    [Fact]
    public async Task Handle_RootDirectoryDoesNotExist_ReturnsEmptyResult()
    {
        _directoryProviderMock.Setup(d => d.DirectoryExists(It.IsAny<string>())).Returns(false);
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(BuildQuery(Combine("does-not-exist")), CancellationToken.None);

        Assert.Equal(0, result.FileCount);
        Assert.Empty(result.FilePaths);
    }

    [Fact]
    public async Task Handle_FileInBlacklistedDirectory_IsExcluded()
    {
        SetupExistingFiles(Combine("root", "bin", "output.dll"), Combine("root", "src", "Program.cs"));
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(
            BuildQuery(Combine("root"), directoryBlacklist: ["bin"]),
            CancellationToken.None);

        Assert.Equal(1, result.FileCount);
        Assert.Equal("Program.cs", Assert.Single(result.FilePaths).Name);
    }

    [Fact]
    public async Task Handle_BlacklistedDirectoryMatch_IsCaseInsensitive()
    {
        SetupExistingFiles(Combine("root", "BIN", "output.dll"), Combine("root", "src", "Program.cs"));
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(
            BuildQuery(Combine("root"), directoryBlacklist: ["bin"]),
            CancellationToken.None);

        Assert.Equal(1, result.FileCount);
        Assert.Equal("Program.cs", Assert.Single(result.FilePaths).Name);
    }

    [Fact]
    public async Task Handle_FileWithBlacklistedExtension_IsExcluded()
    {
        SetupExistingFiles(Combine("root", "lib.dll"), Combine("root", "Program.cs"));
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(
            BuildQuery(Combine("root"), fileExtensionBlacklist: [".dll"]),
            CancellationToken.None);

        Assert.Equal(1, result.FileCount);
        Assert.Equal("Program.cs", Assert.Single(result.FilePaths).Name);
    }

    [Fact]
    public async Task Handle_BlacklistedExtensionMatch_IsCaseInsensitive()
    {
        SetupExistingFiles(Combine("root", "lib.DLL"), Combine("root", "Program.cs"));
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(
            BuildQuery(Combine("root"), fileExtensionBlacklist: [".dll"]),
            CancellationToken.None);

        Assert.Equal(1, result.FileCount);
        Assert.Equal("Program.cs", Assert.Single(result.FilePaths).Name);
    }

    [Fact]
    public async Task Handle_NoBlacklistMatches_ReturnsAllFiles()
    {
        SetupExistingFiles(Combine("root", "a.txt"), Combine("root", "b.txt"));
        var sut = new GetAllFilePathsInDirectoryQueryHandler(_directoryProviderMock.Object);

        var result = await sut.Handle(BuildQuery(Combine("root")), CancellationToken.None);

        Assert.Equal(2, result.FileCount);
    }

    private void SetupExistingFiles(params string[] paths)
    {
        _directoryProviderMock.Setup(d => d.DirectoryExists(It.IsAny<string>())).Returns(true);
        _directoryProviderMock
            .Setup(d => d.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
            .Returns(paths);
    }

    /// <summary>
    /// Builds an OS-appropriate path (backslash-separated on Windows, slash-separated on Unix) purely as a
    /// symbolic value for the mocked <see cref="IDirectoryProvider"/> — nothing here touches the real file
    /// system, but the production blacklist check compares against <see cref="Path.DirectorySeparatorChar"/>,
    /// so a hardcoded Windows-style path (e.g. "C:\root\bin") would silently fail that comparison on Linux CI.
    /// </summary>
    private static string Combine(params string[] segments) => Path.Combine(segments);

    private static GetAllFilePathsInDirectoryQuery BuildQuery(
        string rootDirectory,
        string[]? directoryBlacklist = null,
        string[]? fileExtensionBlacklist = null)
    {
        return new GetAllFilePathsInDirectoryQuery
        {
            RootDirectory = new DirectoryInfo(rootDirectory),
            DirectoryBlacklistItems = (directoryBlacklist ?? [])
                .Select(name => new DirectoryBlacklistItemDto { DirectoryName = name })
                .ToList(),
            FileExtensionBlacklistItems = (fileExtensionBlacklist ?? [])
                .Select(ext => new FileExtensionBlacklistItemDto { Extension = ext })
                .ToList()
        };
    }
}
