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

/// <summary>Verifies the scan loop: which files are scanned, which are skipped, and which events are published in what order.</summary>
public class ProcessScanCommandHandlerTests : IDisposable
{
    private readonly ScanEngineTempDirectory _temp = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IScannerStateService> _stateMock = new();
    private readonly Mock<ILogger<ProcessScanCommandHandler>> _loggerMock = new();
    private readonly CancellationTokenSource _scanCts = new();
    private readonly List<object> _published = [];
    private readonly List<string> _scannedFiles = [];
    private Func<FileInfo, ScanResultDto>? _scanResultFor;

    public ProcessScanCommandHandlerTests()
    {
        _stateMock.SetupGet(s => s.Token).Returns(_scanCts.Token);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns<ScanFileCommand, CancellationToken>((command, _) =>
            {
                _scannedFiles.Add(command.FilePath.Name);
                var result = _scanResultFor?.Invoke(command.FilePath)
                             ?? new ScanResultDto { FilePath = command.FilePath, Warnings = [] };
                return new ValueTask<ScanResultDto>(result);
            });

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<FileProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<FileProcessedEvent, CancellationToken>((e, _) => _published.Add(e))
            .Returns(ValueTask.CompletedTask);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<FoundWarningEvent>(), It.IsAny<CancellationToken>()))
            .Callback<FoundWarningEvent, CancellationToken>((e, _) => _published.Add(e))
            .Returns(ValueTask.CompletedTask);
    }

    public void Dispose()
    {
        _scanCts.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task Handle_TextFiles_ScansEachFileAndPublishesProgress()
    {
        var files = new[] { _temp.WriteText("a.txt", "a"), _temp.WriteText("b.txt", "b"), _temp.WriteText("c.txt", "c") };
        SetupFiles(files);

        var result = await Run();

        Assert.Equal(Unit.Value, result);
        Assert.Equal(["a.txt", "b.txt", "c.txt"], _scannedFiles);
        var progress = _published.OfType<FileProcessedEvent>().ToList();
        Assert.Equal([1, 2, 3], progress.Select(p => p.ScannedFilesCount));
        Assert.All(progress, p => Assert.Equal(3, p.TotalFilesCount));
        Assert.Equal([33, 66, 100], progress.Select(p => p.ProgressInPercent));
    }

    [Fact]
    public async Task Handle_Request_PassesRulesAndSpacyFlagToFileScan()
    {
        SetupFiles(_temp.WriteText("a.txt", "a"));
        var rules = new List<RegexRuleDto> { new() { RuleName = "R", Rule = ".", RuleId = Guid.NewGuid() } };
        ScanFileCommand? sent = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ScanResultDto>, CancellationToken>((c, _) => sent = (ScanFileCommand)c)
            .Returns<ScanFileCommand, CancellationToken>((c, _) =>
                new ValueTask<ScanResultDto>(new ScanResultDto { FilePath = c.FilePath, Warnings = [] }));

        await Run(rules: rules, useSpacy: true);

        Assert.NotNull(sent);
        Assert.Equal("a.txt", sent.FilePath.Name);
        Assert.Same(rules, sent.RegexRuleList);
        Assert.True(sent.UseSpacy);
    }

    [Fact]
    public async Task Handle_Request_PassesRootAndBlacklistsToFileEnumeration()
    {
        SetupFiles();
        var root = new DirectoryInfo(_temp.Path);
        var directories = new List<DirectoryBlacklistItemDto> { new() { DirectoryName = "bin" } };
        var extensions = new List<FileExtensionBlacklistItemDto> { new() { Extension = ".dll" } };
        GetAllFilePathsInDirectoryQuery? sent = null;
        CancellationToken sentToken = default;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllFilePathsInDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetAllFilePathsInDirectoryQueryResult>, CancellationToken>((q, t) =>
            {
                sent = (GetAllFilePathsInDirectoryQuery)q;
                sentToken = t;
            })
            .Returns(new ValueTask<GetAllFilePathsInDirectoryQueryResult>(
                new GetAllFilePathsInDirectoryQueryResult { FilePaths = [], FileCount = 0 }));

        await Run(root: root, directories: directories, extensions: extensions);

        Assert.NotNull(sent);
        Assert.Same(root, sent.RootDirectory);
        Assert.Same(directories, sent.DirectoryBlacklistItems);
        Assert.Same(extensions, sent.FileExtensionBlacklistItems);
        Assert.Equal(_scanCts.Token, sentToken);
    }

    [Fact]
    public async Task Handle_NoFiles_PublishesNothingAndSucceeds()
    {
        SetupFiles();

        var result = await Run();

        Assert.Equal(Unit.Value, result);
        Assert.Empty(_published);
        Assert.Empty(_scannedFiles);
    }

    [Fact]
    public async Task Handle_FileWithWarnings_PublishesProgressBeforeWarning()
    {
        var file = _temp.WriteText("a.txt", "a");
        SetupFiles(file);
        var scanResult = new ScanResultDto { FilePath = file, Warnings = [Warning()] };
        _scanResultFor = _ => scanResult;

        await Run();

        Assert.Collection(
            _published,
            e => Assert.IsType<FileProcessedEvent>(e),
            e => Assert.Same(scanResult, Assert.IsType<FoundWarningEvent>(e).ScanResultDto));
    }

    [Fact]
    public async Task Handle_FileWithoutWarnings_PublishesNoWarningEvent()
    {
        SetupFiles(_temp.WriteText("a.txt", "a"));

        await Run();

        Assert.Empty(_published.OfType<FoundWarningEvent>());
        Assert.Single(_published.OfType<FileProcessedEvent>());
    }

    [Fact]
    public async Task Handle_WarningsOnlyInSecondFile_PublishesOneWarningEventForThatFile()
    {
        var first = _temp.WriteText("a.txt", "a");
        var second = _temp.WriteText("b.txt", "b");
        SetupFiles(first, second);
        _scanResultFor = f => new ScanResultDto
        {
            FilePath = f,
            Warnings = f.Name == "b.txt" ? [Warning()] : []
        };

        await Run();

        var warningEvent = Assert.Single(_published.OfType<FoundWarningEvent>());
        Assert.Equal("b.txt", warningEvent.ScanResultDto.FilePath.Name);
    }

    [Fact]
    public async Task Handle_BinaryFile_IsNotScannedButCountsAsProcessed()
    {
        var binary = _temp.WriteBytes("a.bin", [65, 0, 66]);
        var text = _temp.WriteText("b.txt", "b");
        SetupFiles(binary, text);

        await Run();

        Assert.Equal(["b.txt"], _scannedFiles);
        Assert.Equal([1, 2], _published.OfType<FileProcessedEvent>().Select(p => p.ScannedFilesCount));
        _loggerMock.VerifyLogged(LogLevel.Warning, "a.bin", Times.Once());
    }

    [Fact]
    public async Task Handle_NulByteBeyondSampleSize_IsStillTreatedAsText()
    {
        var content = new byte[8001];
        Array.Fill(content, (byte)'a');
        content[8000] = 0;
        SetupFiles(_temp.WriteBytes("big.dat", content));

        await Run();

        Assert.Equal(["big.dat"], _scannedFiles);
    }

    [Fact]
    public async Task Handle_NulByteWithinSampleSize_IsTreatedAsBinary()
    {
        var content = new byte[8001];
        Array.Fill(content, (byte)'a');
        content[7999] = 0;
        SetupFiles(_temp.WriteBytes("big.dat", content));

        await Run();

        Assert.Empty(_scannedFiles);
    }

    [Fact]
    public async Task Handle_EmptyFile_IsTreatedAsText()
    {
        SetupFiles(_temp.WriteBytes("empty.txt", []));

        await Run();

        Assert.Equal(["empty.txt"], _scannedFiles);
    }

    [Fact]
    public async Task Handle_FileVanishedBeforeRead_IsSkippedWithWarningAndCountsAsProcessed()
    {
        var missing = new FileInfo(Path.Combine(_temp.Path, "gone.txt"));
        var text = _temp.WriteText("b.txt", "b");
        SetupFiles(missing, text);

        await Run();

        Assert.Equal(["b.txt"], _scannedFiles);
        Assert.Equal([1, 2], _published.OfType<FileProcessedEvent>().Select(p => p.ScannedFilesCount));
        _loggerMock.VerifyLogged(LogLevel.Warning, "could not be read", Times.Once());
    }

    [Fact]
    public async Task Handle_CancelledBeforeFirstFile_ScansNothing()
    {
        SetupFiles(_temp.WriteText("a.txt", "a"), _temp.WriteText("b.txt", "b"));
        await _scanCts.CancelAsync();

        var result = await Run();

        Assert.Equal(Unit.Value, result);
        Assert.Empty(_scannedFiles);
        Assert.Empty(_published);
        _loggerMock.VerifyLogged(LogLevel.Information, "cancelled", Times.Once());
    }

    [Fact]
    public async Task Handle_CancelledDuringScan_StopsBeforeNextFile()
    {
        SetupFiles(_temp.WriteText("a.txt", "a"), _temp.WriteText("b.txt", "b"), _temp.WriteText("c.txt", "c"));
        _scanResultFor = f =>
        {
            if (f.Name == "a.txt")
            {
                _scanCts.Cancel();
            }

            return new ScanResultDto { FilePath = f, Warnings = [] };
        };

        await Run();

        Assert.Equal(["a.txt"], _scannedFiles);
        Assert.Single(_published.OfType<FileProcessedEvent>());
    }

    [Fact]
    public async Task Handle_FileCountZeroButFilesListed_ReportsZeroPercent()
    {
        var file = _temp.WriteText("a.txt", "a");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllFilePathsInDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllFilePathsInDirectoryQueryResult>(
                new GetAllFilePathsInDirectoryQueryResult { FilePaths = [file], FileCount = 0 }));

        await Run();

        Assert.Equal(0, Assert.Single(_published.OfType<FileProcessedEvent>()).ProgressInPercent);
    }

    [Fact]
    public async Task Handle_DebugLoggingEnabled_StillScansFile()
    {
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        SetupFiles(_temp.WriteText("a.txt", "a"));

        await Run();

        Assert.Equal(["a.txt"], _scannedFiles);
        _loggerMock.VerifyLogged(LogLevel.Debug, "a.txt", Times.Once());
    }

    [Fact]
    public async Task Handle_Finished_LogsElapsedTime()
    {
        SetupFiles();

        await Run();

        _loggerMock.VerifyLogged(LogLevel.Information, "Finished scan", Times.Once());
    }

    private void SetupFiles(params FileInfo[] files) =>
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllFilePathsInDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllFilePathsInDirectoryQueryResult>(
                new GetAllFilePathsInDirectoryQueryResult { FilePaths = files, FileCount = files.Length }));

    private async ValueTask<Unit> Run(
        DirectoryInfo? root = null,
        List<RegexRuleDto>? rules = null,
        List<DirectoryBlacklistItemDto>? directories = null,
        List<FileExtensionBlacklistItemDto>? extensions = null,
        bool useSpacy = false)
    {
        var sut = new ProcessScanCommandHandler(_mediatorMock.Object, _stateMock.Object, _loggerMock.Object);

        return await sut.Handle(
            new ProcessScanCommand
            {
                RootDirectory = root ?? new DirectoryInfo(_temp.Path),
                RegexRuleList = rules,
                DirectoryBlacklistItems = directories ?? [],
                FileExtensionBlacklistItems = extensions ?? [],
                UseSpacy = useSpacy
            },
            CancellationToken.None);
    }

    private static ScanWarningDto Warning() =>
        new()
        {
            Line = 1,
            Start = 0,
            End = 1,
            SuspiciousContent = new SuspiciousContentDto
            {
                MatchText = "a",
                PrevLine = null,
                HitLine = "a",
                NextLine = null
            },
            Type = ScanWarningType.Rule,
            RuleId = null,
            SpacyLabel = null
        };
}
