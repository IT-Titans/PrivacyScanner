using Microsoft.Extensions.Logging;
using Moq;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>A per-test temp directory that deletes itself on dispose. Used by the scan engine tests that need real files.</summary>
internal sealed class ScanEngineTempDirectory : IDisposable
{
    public ScanEngineTempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrivacyScannerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public FileInfo WriteText(string name, string content)
    {
        var fullName = System.IO.Path.Combine(Path, name);
        File.WriteAllText(fullName, content);
        return new FileInfo(fullName);
    }

    public FileInfo WriteBytes(string name, byte[] content)
    {
        var fullName = System.IO.Path.Combine(Path, name);
        File.WriteAllBytes(fullName, content);
        return new FileInfo(fullName);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

internal static class ScanEngineLoggerExtensions
{
    /// <summary>Verifies how often a log entry of the given level whose formatted message contains the text was written.</summary>
    public static void VerifyLogged<T>(this Mock<ILogger<T>> logger, LogLevel level, string messagePart, Times times)
    {
        logger.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state != null && state.ToString()!.Contains(messagePart, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
