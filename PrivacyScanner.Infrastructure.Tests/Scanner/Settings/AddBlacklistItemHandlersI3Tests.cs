using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>Verifies the two add handlers: what they pass on to the save command and what ends up on disk.</summary>
public sealed class AddBlacklistItemHandlersI3Tests : IDisposable
{
    private readonly SettingsHandlerTestDirectory _settings = new();

    public void Dispose() => _settings.Dispose();

    private static Mock<IMediator> MediatorWith(GetAllBlacklistItemsQueryResult current)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllBlacklistItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllBlacklistItemsQueryResult>(current));
        mediator
            .Setup(m => m.Send(It.IsAny<SaveDirectoryBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Unit>(Unit.Value));
        mediator
            .Setup(m => m.Send(It.IsAny<SaveFileExtensionBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Unit>(Unit.Value));
        return mediator;
    }

    private static GetAllBlacklistItemsQueryResult Current(string[] directories, string[] extensions) => new()
    {
        DirectoryBlacklistItems = directories.Select(d => new DirectoryBlacklistItemDto { DirectoryName = d }).ToList(),
        FileExtensionBlacklistItems = extensions.Select(e => new FileExtensionBlacklistItemDto { Extension = e }).ToList(),
    };

    [Fact]
    public async Task AddDirectory_AppendsTheNewNameAndSavesTheWholeList()
    {
        var mediator = MediatorWith(Current(["bin", "obj"], []));

        await new AddDirectoryBlacklistItemCommandHandler(mediator.Object).Handle(
            new AddDirectoryBlacklistItemCommand { DirectoryName = "archiv" }, CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SaveDirectoryBlacklistItemsStatesCommand>(c => c.DirectoryBlacklistItems.Select(i => i.DirectoryName).SequenceEqual(new[] { "bin", "obj", "archiv" })),
            It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.IsAny<SaveFileExtensionBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddDirectory_NameAlreadyListed_IsSavedOnlyOnce()
    {
        var mediator = MediatorWith(Current(["bin", "obj"], []));

        await new AddDirectoryBlacklistItemCommandHandler(mediator.Object).Handle(
            new AddDirectoryBlacklistItemCommand { DirectoryName = "bin" }, CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SaveDirectoryBlacklistItemsStatesCommand>(c => c.DirectoryBlacklistItems.Select(i => i.DirectoryName).SequenceEqual(new[] { "bin", "obj" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddDirectory_ReturnsUnit()
    {
        var result = await new AddDirectoryBlacklistItemCommandHandler(MediatorWith(Current([], [])).Object).Handle(
            new AddDirectoryBlacklistItemCommand { DirectoryName = "x" }, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task AddDirectory_OnFreshInstallation_PersistsTheDefaultsPlusTheNewName()
    {
        await new AddDirectoryBlacklistItemCommandHandler(SettingsHandlerMediator.Create(_settings.Provider)).Handle(
            new AddDirectoryBlacklistItemCommand { DirectoryName = "archiv" }, CancellationToken.None);

        var saved = SettingsHandlerMediator.ReadList<DirectoryBlacklistItemDto>(_settings.DirectoryBlacklistFile).Select(i => i.DirectoryName).ToList();
        Assert.Equal("archiv", saved[^1]);
        Assert.Contains("node_modules", saved);
        Assert.Equal(8, saved.Count);
        Assert.False(File.Exists(_settings.FileExtensionBlacklistFile));
    }

    [Fact]
    public async Task AddDirectory_Twice_PersistsTheNameOnce()
    {
        var sut = new AddDirectoryBlacklistItemCommandHandler(SettingsHandlerMediator.Create(_settings.Provider));

        await sut.Handle(new AddDirectoryBlacklistItemCommand { DirectoryName = "archiv" }, CancellationToken.None);
        await sut.Handle(new AddDirectoryBlacklistItemCommand { DirectoryName = "archiv" }, CancellationToken.None);

        Assert.Single(SettingsHandlerMediator.ReadList<DirectoryBlacklistItemDto>(_settings.DirectoryBlacklistFile), i => i.DirectoryName == "archiv");
    }

    [Fact]
    public async Task AddFileExtension_AppendsTheNewExtensionAndSavesTheWholeList()
    {
        var mediator = MediatorWith(Current([], [".dll", ".exe"]));

        await new AddFileExtensionBlacklistItemCommandHandler(mediator.Object).Handle(
            new AddFileExtensionBlacklistItemCommand { Extension = ".bak" }, CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SaveFileExtensionBlacklistItemsStatesCommand>(c => c.FileExtensionBlacklistItems.Select(i => i.Extension).SequenceEqual(new[] { ".dll", ".exe", ".bak" })),
            It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.IsAny<SaveDirectoryBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddFileExtension_ExtensionAlreadyListed_IsSavedOnlyOnce()
    {
        var mediator = MediatorWith(Current([], [".dll", ".exe"]));

        await new AddFileExtensionBlacklistItemCommandHandler(mediator.Object).Handle(
            new AddFileExtensionBlacklistItemCommand { Extension = ".dll" }, CancellationToken.None);

        mediator.Verify(m => m.Send(
            It.Is<SaveFileExtensionBlacklistItemsStatesCommand>(c => c.FileExtensionBlacklistItems.Select(i => i.Extension).SequenceEqual(new[] { ".dll", ".exe" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddFileExtension_ReturnsUnit()
    {
        var result = await new AddFileExtensionBlacklistItemCommandHandler(MediatorWith(Current([], [])).Object).Handle(
            new AddFileExtensionBlacklistItemCommand { Extension = ".x" }, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task AddFileExtension_OnFreshInstallation_PersistsTheDefaultsPlusTheNewExtension()
    {
        await new AddFileExtensionBlacklistItemCommandHandler(SettingsHandlerMediator.Create(_settings.Provider)).Handle(
            new AddFileExtensionBlacklistItemCommand { Extension = ".bak" }, CancellationToken.None);

        var saved = SettingsHandlerMediator.ReadList<FileExtensionBlacklistItemDto>(_settings.FileExtensionBlacklistFile).Select(i => i.Extension).ToList();
        Assert.Equal(".bak", saved[^1]);
        Assert.Contains(".dll", saved);
        Assert.Equal(5, saved.Count);
        Assert.False(File.Exists(_settings.DirectoryBlacklistFile));
    }
}
