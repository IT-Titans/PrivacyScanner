using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies that starting a scan resets the scanner state first and hands every setting on, and that stopping cancels it.</summary>
public class StartStopScannerCommandHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IScannerStateService> _stateMock = new();

    [Fact]
    public async Task Start_ResetsStateBeforeTheScanBegins()
    {
        var calls = new List<string>();
        _stateMock.Setup(s => s.Reset()).Callback(() => calls.Add("reset"));
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessScanCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((_, _) => calls.Add("scan"))
            .Returns(new ValueTask<Unit>(Unit.Value));
        var sut = new StartScannerCommandHandler(_mediatorMock.Object, _stateMock.Object);

        var result = await sut.Handle(BuildStartCommand(), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal(["reset", "scan"], calls);
    }

    [Fact]
    public async Task Start_ForwardsAllSettingsToTheScan()
    {
        ProcessScanCommand? sent = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessScanCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((c, _) => sent = (ProcessScanCommand)c)
            .Returns(new ValueTask<Unit>(Unit.Value));
        var sut = new StartScannerCommandHandler(_mediatorMock.Object, _stateMock.Object);
        var command = BuildStartCommand();

        await sut.Handle(command, CancellationToken.None);

        Assert.NotNull(sent);
        Assert.Same(command.RootDirectory, sent.RootDirectory);
        Assert.Same(command.RegexRuleList, sent.RegexRuleList);
        Assert.Same(command.DirectoryBlacklistItems, sent.DirectoryBlacklistItems);
        Assert.Same(command.FileExtensionBlacklistItems, sent.FileExtensionBlacklistItems);
        Assert.True(sent.UseSpacy);
    }

    [Fact]
    public async Task Start_PassesTheCallersTokenOn()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken sentToken = default;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessScanCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((_, t) => sentToken = t)
            .Returns(new ValueTask<Unit>(Unit.Value));
        var sut = new StartScannerCommandHandler(_mediatorMock.Object, _stateMock.Object);

        await sut.Handle(BuildStartCommand(), cts.Token);

        Assert.Equal(cts.Token, sentToken);
    }

    [Fact]
    public async Task Stop_CancelsTheRunningScan()
    {
        var sut = new StopScannerCommandHandler(_stateMock.Object);

        var result = await sut.Handle(new StopScannerCommand(), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _stateMock.Verify(s => s.Cancel(), Times.Once);
        _stateMock.Verify(s => s.Reset(), Times.Never);
    }

    private static StartScannerCommand BuildStartCommand() =>
        new()
        {
            RootDirectory = new DirectoryInfo(Path.GetTempPath()),
            RegexRuleList = [new RegexRuleDto { RuleName = "R", Rule = ".", RuleId = Guid.NewGuid() }],
            DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }],
            FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".dll" }],
            UseSpacy = true
        };
}
