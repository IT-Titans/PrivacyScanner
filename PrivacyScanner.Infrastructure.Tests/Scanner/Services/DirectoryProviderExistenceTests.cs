using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services;

/// <summary>Verifies <see cref="DirectoryProvider.DirectoryExists"/> and the search pattern handling of <see cref="DirectoryProvider.GetFiles"/>.</summary>
public class DirectoryProviderExistenceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"privacyscanner-dirprovider-exists-{Guid.NewGuid()}");
    private readonly DirectoryProvider _sut = new(Mock.Of<ILogger<DirectoryProvider>>());

    public DirectoryProviderExistenceTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        Assert.True(_sut.DirectoryExists(_rootPath));
    }

    [Fact]
    public void DirectoryExists_MissingDirectory_ReturnsFalse()
    {
        Assert.False(_sut.DirectoryExists(Path.Combine(_rootPath, "missing")));
    }

    [Fact]
    public void DirectoryExists_PathOfAFile_ReturnsFalse()
    {
        var file = Path.Combine(_rootPath, "file.txt");
        File.WriteAllText(file, "x");

        Assert.False(_sut.DirectoryExists(file));
    }

    [Fact]
    public void GetFiles_WithSearchPattern_ReturnsOnlyMatchingFilesFromAllLevels()
    {
        File.WriteAllText(Path.Combine(_rootPath, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_rootPath, "a.log"), "a");
        var sub = Directory.CreateDirectory(Path.Combine(_rootPath, "sub"));
        File.WriteAllText(Path.Combine(sub.FullName, "b.txt"), "b");
        File.WriteAllText(Path.Combine(sub.FullName, "b.log"), "b");

        var files = _sut.GetFiles(_rootPath, "*.txt", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(new[] { "a.txt", "b.txt" }, files);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
