using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>
/// Covers the guard clauses, the process invocation and the "nothing usable came back" branches of
/// <see cref="SpaCyScanCommandHandler"/>. The JSON-to-warning mapping is covered in <c>PrivacyScanner.Tests</c>.
/// </summary>
public class SpaCyScanCommandHandlerEnvironmentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"privacyscanner-spacy-{Guid.NewGuid()}");
    private readonly Mock<IProcessService> _processService = new();
    private readonly SpaCyScanCommandHandler _sut;

    public SpaCyScanCommandHandlerEnvironmentTests()
    {
        Directory.CreateDirectory(_directory);
        _sut = new SpaCyScanCommandHandler(_processService.Object, Mock.Of<ILogger<SpaCyScanCommandHandler>>());
    }

    private FileInfo CreateInput(string content = "Max Mustermann wohnt in Berlin.")
    {
        var path = Path.Combine(_directory, "input.txt");
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    private void SetupProcess(int exitCode, string output, string error = "") =>
        _processService.Setup(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((exitCode, output, error));

    private ValueTask<ScanResultDto> Run(FileInfo file) =>
        _sut.Handle(new SpaCyScanCommand { FilePath = file }, CancellationToken.None);

    [Fact]
    public void Constructor_NullProcessService_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SpaCyScanCommandHandler(null!, Mock.Of<ILogger<SpaCyScanCommandHandler>>()));

        Assert.Equal("processService", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SpaCyScanCommandHandler(_processService.Object, null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public async Task Handle_FileDoesNotExist_ReturnsEmptyResultWithoutStartingPython()
    {
        var missing = new FileInfo(Path.Combine(_directory, "missing.txt"));

        var result = await Run(missing);

        Assert.Empty(result.Warnings);
        Assert.Same(missing, result.FilePath);
        _processService.Verify(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Cancelled_PropagatesAndLogsNoError()
    {
        var logger = new Mock<ILogger<SpaCyScanCommandHandler>>();
        var sut = new SpaCyScanCommandHandler(_processService.Object, logger.Object);
        var input = CreateInput();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await sut.Handle(new SpaCyScanCommand { FilePath = input }, cts.Token));

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        _processService.Verify(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingFile_RunsPythonLauncherWithScriptAndQuotedFilePath()
    {
        var input = CreateInput();
        SetupProcess(0, "{\"entities\": []}");

        var result = await Run(input);

        Assert.Empty(result.Warnings);
        _processService.Verify(p => p.RunCommandAsync(
            "py",
            It.Is<string>(a => a.StartsWith("-3.12 \"", StringComparison.Ordinal)
                               && a.Contains("spacy_scan.py\"", StringComparison.Ordinal)
                               && a.EndsWith($"\"{input.FullName}\"", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PythonExitsNonZero_IgnoresOutputAndReturnsNoWarnings()
    {
        var entity = "{\"entities\": [{\"text\": \"Max\", \"label\": \"PER\", \"hit_line_position\": 1}]}";
        SetupProcess(1, entity, "Traceback");

        var result = await Run(CreateInput());

        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n")]
    public async Task Handle_PythonProducesNoOutput_ReturnsNoWarnings(string output)
    {
        SetupProcess(0, output);

        var result = await Run(CreateInput());

        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"entities\": null}")]
    [InlineData("null")]
    public async Task Handle_JsonWithoutEntityList_ReturnsNoWarnings(string output)
    {
        SetupProcess(0, output);

        var result = await Run(CreateInput());

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_EntityWithUnknownLabelAndMixedCasePropertyNames_IsMappedToUnknownLabel()
    {
        SetupProcess(0, "{\"Entities\": [{\"text\": \"Foo\", \"Label\": \"FANTASY\", \"hit_line_position\": 4, \"start\": 2, \"end\": 5}]}");

        var result = await Run(CreateInput());

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(SpaCyLabel.Unknown, warning.SpacyLabel);
        Assert.Equal(ScanWarningType.SpaCy, warning.Type);
        Assert.Equal(4, warning.Line);
        Assert.Equal(2, warning.Start);
        Assert.Equal(5, warning.End);
        Assert.Equal("Foo", warning.SuspiciousContent.MatchText);
    }

    [Fact]
    public async Task Handle_ProcessServiceThrows_ReturnsEmptyResultInsteadOfPropagating()
    {
        _processService.Setup(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await Run(CreateInput());

        Assert.Empty(result.Warnings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
