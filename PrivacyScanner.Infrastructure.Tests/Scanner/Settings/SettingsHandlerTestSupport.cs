using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>
/// A private settings directory per test, served through <see cref="ISettingsPathProvider"/>, that deletes
/// itself on dispose. The directory is not created up front, because the real one may not exist either.
/// </summary>
internal sealed class SettingsHandlerTestDirectory : IDisposable
{
    public SettingsHandlerTestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrivacyScannerSettingsTests-" + Guid.NewGuid().ToString("N"));
        Provider = Mock.Of<ISettingsPathProvider>(p => p.GetSettingsDirectory() == Path);
    }

    public string Path { get; }

    public ISettingsPathProvider Provider { get; }

    public string RulesFile => System.IO.Path.Combine(Path, "rules.json");

    public string DirectoryBlacklistFile => System.IO.Path.Combine(Path, "directoryBlacklists.json");

    public string FileExtensionBlacklistFile => System.IO.Path.Combine(Path, "fileExtensionBlacklists.json");

    public void WriteFile(string file, string content)
    {
        Directory.CreateDirectory(Path);
        File.WriteAllText(file, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

/// <summary>
/// An <see cref="IMediator"/> that hands the settings requests to the real handlers working on a temp
/// directory, so that a command handler built on <c>IMediator</c> is checked against what really lands on disk.
/// </summary>
internal static class SettingsHandlerMediator
{
    public static IMediator Create(ISettingsPathProvider provider)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllBlacklistItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns((GetAllBlacklistItemsQuery q, CancellationToken ct) =>
                new GetAllBlacklistItemsQueryHandler(Mock.Of<ILogger<GetAllBlacklistItemsQueryHandler>>(), provider).Handle(q, ct));
        mediator
            .Setup(m => m.Send(It.IsAny<SaveDirectoryBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()))
            .Returns((SaveDirectoryBlacklistItemsStatesCommand c, CancellationToken ct) =>
                new SaveDirectoryBlacklistItemsStatesCommandHandler(provider).Handle(c, ct));
        mediator
            .Setup(m => m.Send(It.IsAny<SaveFileExtensionBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()))
            .Returns((SaveFileExtensionBlacklistItemsStatesCommand c, CancellationToken ct) =>
                new SaveFileExtensionBlacklistItemsStatesCommandHandler(provider).Handle(c, ct));
        return mediator.Object;
    }

    public static List<T> ReadList<T>(string file) =>
        JsonSerializer.Deserialize<List<T>>(File.ReadAllText(file))!;
}
