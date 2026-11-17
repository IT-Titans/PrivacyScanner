using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Helpers;

public class SpaCyScriptLocatorTests : IDisposable
{
    private static readonly System.Reflection.Assembly Assembly = typeof(SpaCyScriptLocator).Assembly;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"privacyscanner-locator-{Guid.NewGuid()}");
    private readonly string _extraction;

    public SpaCyScriptLocatorTests()
    {
        _extraction = Path.Combine(_root, "extracted");
        Directory.CreateDirectory(_root);
    }

    private static byte[] EmbeddedScript()
    {
        using var stream = Assembly.GetManifestResourceStream(SpaCyScriptLocator.ResourceName)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private Task<string> Locate(string? assemblyDirectory = null, string resourceName = SpaCyScriptLocator.ResourceName, string? extraction = null) =>
        SpaCyScriptLocator.GetScriptPathAsync(Assembly, assemblyDirectory, extraction ?? _extraction, resourceName, CancellationToken.None);

    [Fact]
    public async Task GetScriptPathAsync_ConcurrentCallers_EveryCallerSeesTheCompleteScript()
    {
        var expected = EmbeddedScript();
        var mismatches = 0;
        var directories = new List<string>();

        for (var round = 0; round < 25; round++)
        {
            var directory = Path.Combine(_extraction, round.ToString());
            directories.Add(directory);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var callers = Enumerable.Range(0, 16).Select(async _ =>
            {
                await gate.Task;
                var path = await Locate(extraction: directory);
                // Read at once: a half-written or truncated file shows up here. Full sharing, because the
                // handle another caller briefly holds while it publishes the file is not what is under test.
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var content = new byte[stream.Length];
                await stream.ReadExactlyAsync(content);
                return (path, content);
            }).ToArray();
            gate.SetResult();

            foreach (var (path, content) in await Task.WhenAll(callers))
            {
                if (!content.AsSpan().SequenceEqual(expected))
                {
                    mismatches++;
                }
            }
        }

        Assert.Equal(0, mismatches);
        Assert.All(directories, d => Assert.Single(Directory.GetFiles(d, "*", SearchOption.AllDirectories)));
    }

    [Fact]
    public async Task GetScriptPathAsync_ExtractsToHashNamedDirectoryAndLeavesNoPartialFile()
    {
        var path = await Locate();

        Assert.Equal("spacy_scan.py", Path.GetFileName(path));
        var hashDirectory = Path.GetDirectoryName(path)!;
        Assert.Matches("^[0-9a-f]{16}$", Path.GetFileName(hashDirectory));
        Assert.Equal(_extraction, Path.GetDirectoryName(hashDirectory));
        Assert.Equal(EmbeddedScript(), await File.ReadAllBytesAsync(path));
        Assert.Equal(new[] { path }, Directory.GetFiles(_extraction, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task GetScriptPathAsync_SecondCall_ReusesTheExtractedFile()
    {
        var first = await Locate();
        var written = File.GetLastWriteTimeUtc(first);
        File.SetLastWriteTimeUtc(first, written.AddHours(-1));

        var second = await Locate();

        Assert.Equal(first, second);
        Assert.Equal(written.AddHours(-1), File.GetLastWriteTimeUtc(second));
        Assert.Single(Directory.GetFiles(_extraction, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task GetScriptPathAsync_ExistingFileHasOtherContent_ReplacesIt()
    {
        var path = await Locate();
        await File.WriteAllTextAsync(path, "print('truncated");

        var again = await Locate();

        Assert.Equal(path, again);
        Assert.Equal(EmbeddedScript(), await File.ReadAllBytesAsync(again));
        Assert.Single(Directory.GetFiles(_extraction, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task GetScriptPathAsync_ScriptBesideAssembly_IsUsedAndNothingIsExtracted()
    {
        var resources = Path.Combine(_root, "assembly", "Resources");
        Directory.CreateDirectory(resources);
        var local = Path.Combine(resources, "spacy_scan.py");
        await File.WriteAllTextAsync(local, "# local override");

        var path = await Locate(Path.Combine(_root, "assembly"));

        Assert.Equal(local, path);
        Assert.False(Directory.Exists(_extraction));
    }

    [Fact]
    public async Task GetScriptPathAsync_AssemblyDirectoryWithoutScript_FallsBackToEmbeddedResource()
    {
        var path = await Locate(Path.Combine(_root, "assembly-without-resources"));

        Assert.StartsWith(_extraction, path);
    }

    [Fact]
    public async Task GetScriptPathAsync_MissingResource_ThrowsAndNamesWhatIsAvailable()
    {
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => Locate(resourceName: "No.Such.Resource.py"));

        Assert.Contains("No.Such.Resource.py", ex.Message);
        Assert.Contains(SpaCyScriptLocator.ResourceName, ex.Message);
        Assert.False(Directory.Exists(_extraction));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
