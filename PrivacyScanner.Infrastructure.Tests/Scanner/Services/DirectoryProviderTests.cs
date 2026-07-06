using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services;

/// <summary>Verifies recursive file collection and its resilience to inaccessible/missing directories.</summary>
public class DirectoryProviderTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"privacyscanner-dirprovider-{Guid.NewGuid()}");
    private readonly DirectoryProvider _sut = new(Mock.Of<ILogger<DirectoryProvider>>());

    public DirectoryProviderTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void GetFiles_WithAllDirectories_ReturnsFilesFromNestedSubdirectories()
    {
        File.WriteAllText(Path.Combine(_rootPath, "root.txt"), "root");
        var subDirectory = Directory.CreateDirectory(Path.Combine(_rootPath, "sub"));
        File.WriteAllText(Path.Combine(subDirectory.FullName, "nested.txt"), "nested");

        var files = _sut.GetFiles(_rootPath, "*.*", SearchOption.AllDirectories).ToList();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.EndsWith("root.txt"));
        Assert.Contains(files, f => f.EndsWith("nested.txt"));
    }

    [Fact]
    public void GetFiles_WithTopDirectoryOnly_DoesNotRecurseIntoSubdirectories()
    {
        File.WriteAllText(Path.Combine(_rootPath, "root.txt"), "root");
        var subDirectory = Directory.CreateDirectory(Path.Combine(_rootPath, "sub"));
        File.WriteAllText(Path.Combine(subDirectory.FullName, "nested.txt"), "nested");

        var files = _sut.GetFiles(_rootPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

        Assert.Single(files);
        Assert.EndsWith("root.txt", files[0]);
    }

    [Fact]
    public void GetFiles_NonExistentDirectory_ReturnsEmptyInsteadOfThrowing()
    {
        var nonExistentPath = Path.Combine(_rootPath, "does-not-exist");

        var files = _sut.GetFiles(nonExistentPath, "*.*", SearchOption.AllDirectories);

        Assert.Empty(files);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
