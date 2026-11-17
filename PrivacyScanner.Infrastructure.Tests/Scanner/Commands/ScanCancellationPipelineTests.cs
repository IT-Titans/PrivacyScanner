using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>
/// Runs the real scan chain (process scan, file scan, regex scan) on real files and checks how cancellation and
/// genuine read errors are reported. Only the mediator wiring between the handlers is mocked.
/// </summary>
public class ScanCancellationPipelineTests : IDisposable
{
    private readonly ScanEngineTempDirectory _temp = new();
    private readonly CancellationTokenSource _scanCts = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IScannerStateService> _state = new();
    private readonly Mock<ILogger<ScanFileCommandHandler>> _fileLogger = new();
    private readonly Mock<ILogger<ProcessScanCommandHandler>> _processLogger = new();
    private readonly List<string> _processed = [];
    private readonly List<string> _regexScanned = [];
    private readonly RegexScanCommandHandler _regexHandler;
    private Action<FileInfo>? _onRegexScan;

    public ScanCancellationPipelineTests()
    {
        _state.SetupGet(s => s.Token).Returns(_scanCts.Token);
        _mediator.Setup(m => m.Publish(It.IsAny<FileProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _mediator.Setup(m => m.Publish(It.IsAny<FoundWarningEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var fileHandler = new ScanFileCommandHandler(_mediator.Object, _fileLogger.Object);
        _regexHandler = new RegexScanCommandHandler(_processLogger.Object);

        _mediator.Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns<ScanFileCommand, CancellationToken>((c, t) =>
            {
                _processed.Add(c.FilePath.Name);
                return fileHandler.Handle(c, t);
            });
        _mediator.Setup(m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()))
            .Returns<RegexScanCommand, CancellationToken>((c, t) =>
            {
                _regexScanned.Add(c.FilePath.Name);
                _onRegexScan?.Invoke(c.FilePath);
                return _regexHandler.Handle(c, t);
            });
    }

    public void Dispose()
    {
        _scanCts.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task Scan_CancelledWhileRegexScanRuns_ThrowsCancellationAndLogsNoError()
    {
        SetupFiles(_temp.WriteText("a.txt", "secret"), _temp.WriteText("b.txt", "secret"));
        _onRegexScan = _ => _scanCts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Run());

        Assert.Equal(["a.txt"], _processed);
        AssertNoErrorLogged();
    }

    [Fact]
    public async Task Scan_CancelRequestedBetweenFiles_StopsWithoutErrorOrException()
    {
        SetupFiles(_temp.WriteText("a.txt", "secret"), _temp.WriteText("b.txt", "secret"));
        await _scanCts.CancelAsync();

        await Run();

        Assert.Empty(_processed);
        AssertNoErrorLogged();
    }

    [Fact]
    public async Task Scan_RealErrorInOneFile_IsLoggedAndOtherFilesAreStillScanned()
    {
        SetupFiles(_temp.WriteText("a.txt", "secret"), _temp.WriteText("b.txt", "secret"), _temp.WriteText("c.txt", "secret"));
        // The binary check opens the file first and skips unreadable ones, so the read error is raised in the regex scan.
        _onRegexScan = file =>
        {
            if (file.Name == "b.txt")
            {
                throw new IOException("locked");
            }
        };

        await Run();

        Assert.Equal(["a.txt", "b.txt", "c.txt"], _processed);
        Assert.Equal(["a.txt", "b.txt", "c.txt"], _regexScanned);
        _fileLogger.VerifyLogged(LogLevel.Error, "b.txt", Times.Once());
        _fileLogger.VerifyLogged(LogLevel.Error, "a.txt", Times.Never());
        _fileLogger.VerifyLogged(LogLevel.Error, "c.txt", Times.Never());
    }

    private void AssertNoErrorLogged()
    {
        _fileLogger.VerifyLogged(LogLevel.Error, "", Times.Never());
        _processLogger.VerifyLogged(LogLevel.Error, "", Times.Never());
    }

    private void SetupFiles(params FileInfo[] files) =>
        _mediator.Setup(m => m.Send(It.IsAny<GetAllFilePathsInDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllFilePathsInDirectoryQueryResult>(
                new GetAllFilePathsInDirectoryQueryResult { FilePaths = files, FileCount = files.Length }));

    private async Task Run()
    {
        var sut = new ProcessScanCommandHandler(_mediator.Object, _state.Object, _processLogger.Object);
        await sut.Handle(
            new ProcessScanCommand
            {
                RootDirectory = new DirectoryInfo(_temp.Path),
                RegexRuleList = [new RegexRuleDto { RuleName = "Rule", Rule = "secret", RuleId = Guid.NewGuid() }],
                DirectoryBlacklistItems = [],
                FileExtensionBlacklistItems = [],
                UseSpacy = false
            },
            CancellationToken.None);
    }
}
