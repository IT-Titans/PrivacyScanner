using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Queries;

/// <summary>
/// Verifies the three-step Python/spaCy environment check against a mocked <see cref="IProcessService"/>.
/// The first call is only matched on the executable and the arguments of the later calls are asserted exactly;
/// the first call's arguments (<c>py --version</c> instead of <c>py -3.12 --version</c>) are a known suspect
/// in the work package (section 5.7) and deliberately not pinned here.
/// </summary>
public class CheckPythonAndSpacyEnvironmentQueryHandlerTests
{
    private const string PipShowArguments = "-3.12 -m pip show spacy";
    private const string SpacyInfoArguments = "-3.12 -m spacy info de_core_news_sm";

    private readonly Mock<IProcessService> _processService = new(MockBehavior.Strict);
    private readonly CheckPythonAndSpacyEnvironmentQueryHandler _sut;

    public CheckPythonAndSpacyEnvironmentQueryHandlerTests()
    {
        _sut = new CheckPythonAndSpacyEnvironmentQueryHandler(_processService.Object);
    }

    private void SetupPython(int exitCode, string output = "", string error = "") =>
        _processService.Setup(p => p.RunCommandAsync("py", It.Is<string>(a => a.EndsWith("--version", StringComparison.Ordinal))))
            .ReturnsAsync((exitCode, output, error));

    private void SetupPipShow(int exitCode, string error = "") =>
        _processService.Setup(p => p.RunCommandAsync("py", PipShowArguments)).ReturnsAsync((exitCode, string.Empty, error));

    private void SetupSpacyInfo(int exitCode) =>
        _processService.Setup(p => p.RunCommandAsync("py", SpacyInfoArguments)).ReturnsAsync((exitCode, string.Empty, string.Empty));

    private Task<CheckPythonAndSpacyEnvironmentQueryResult> RunAsync() =>
        _sut.Handle(new CheckPythonAndSpacyEnvironmentQuery(), CancellationToken.None).AsTask();

    [Fact]
    public async Task Handle_PythonSpacyAndModelPresent_ReturnsSuccessWithoutMessages()
    {
        SetupPython(0, "Python 3.12.4\r\n");
        SetupPipShow(0);
        SetupSpacyInfo(0);

        var result = await RunAsync();

        Assert.True(result.IsValid);
        Assert.Null(result.UserMessage);
        Assert.Null(result.TechnicalMessage);
        _processService.Verify(p => p.RunCommandAsync("py", PipShowArguments), Times.Once);
        _processService.Verify(p => p.RunCommandAsync("py", SpacyInfoArguments), Times.Once);
    }

    [Fact]
    public async Task Handle_PythonVersionOnStandardError_IsAccepted()
    {
        // Older Python 3 builds print the version banner to stderr.
        SetupPython(0, string.Empty, "Python 3.12.1");
        SetupPipShow(0);
        SetupSpacyInfo(0);

        var result = await RunAsync();

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Handle_PythonCommandFails_ReportsMissingPythonAndStopsEarly()
    {
        SetupPython(9009, string.Empty, "py is not recognized");

        var result = await RunAsync();

        Assert.False(result.IsValid);
        Assert.Contains("Python 3.12 konnte nicht gefunden werden", result.UserMessage);
        Assert.Equal("ExitCode: 9009, Error: py is not recognized", result.TechnicalMessage);
        _processService.Verify(p => p.RunCommandAsync("py", PipShowArguments), Times.Never);
        _processService.Verify(p => p.RunCommandAsync("py", SpacyInfoArguments), Times.Never);
    }

    [Theory]
    [InlineData("Python 3.11.9")]
    [InlineData("Python 3.13.0")]
    [InlineData("Python 3.12")]
    [InlineData("")]
    [InlineData("something else entirely")]
    public async Task Handle_PythonVersionIsNotThreeTwelve_ReportsUnrecognisedVersion(string versionOutput)
    {
        SetupPython(0, versionOutput);

        var result = await RunAsync();

        Assert.False(result.IsValid);
        Assert.Equal("Die installierte Python-Version konnte nicht korrekt erkannt werden.", result.UserMessage);
        Assert.Equal($"Output: {versionOutput.Trim()}", result.TechnicalMessage);
        _processService.Verify(p => p.RunCommandAsync("py", PipShowArguments), Times.Never);
    }

    [Fact]
    public async Task Handle_SpacyNotInstalled_ReportsInstallHintAndSkipsModelCheck()
    {
        SetupPython(0, "Python 3.12.4");
        SetupPipShow(1, "WARNING: Package(s) not found: spacy");

        var result = await RunAsync();

        Assert.False(result.IsValid);
        Assert.Contains("spaCy ist nicht installiert", result.UserMessage);
        Assert.Contains("py -3.12 -m pip install spacy", result.UserMessage);
        Assert.Equal("ExitCode: 1, Error: WARNING: Package(s) not found: spacy", result.TechnicalMessage);
        _processService.Verify(p => p.RunCommandAsync("py", SpacyInfoArguments), Times.Never);
    }

    [Fact]
    public async Task Handle_GermanModelMissing_ReportsDownloadHint()
    {
        SetupPython(0, "Python 3.12.4");
        SetupPipShow(0);
        SetupSpacyInfo(1);

        var result = await RunAsync();

        Assert.False(result.IsValid);
        Assert.Equal("Das spaCy Sprachmodell 'de_core_news_sm' ist nicht installiert.", result.UserMessage);
        Assert.Equal("Bitte führen Sie 'py -3.12 -m spacy download de_core_news_sm' aus.", result.TechnicalMessage);
    }

    [Fact]
    public async Task Handle_ProcessServiceThrows_ReturnsFailureWithExceptionMessage()
    {
        _processService.Setup(p => p.RunCommandAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await RunAsync();

        Assert.False(result.IsValid);
        Assert.Equal("Ein unerwarteter Fehler ist bei der Umgebungsprüfung aufgetreten.", result.UserMessage);
        Assert.Equal("boom", result.TechnicalMessage);
    }

    [Fact]
    public void Result_SuccessAndFailure_CarryTheirFlagsAndMessages()
    {
        var success = CheckPythonAndSpacyEnvironmentQueryResult.Success();
        var failure = CheckPythonAndSpacyEnvironmentQueryResult.Failure("user");
        var detailed = CheckPythonAndSpacyEnvironmentQueryResult.Failure("user", "technical");

        Assert.True(success.IsValid);
        Assert.False(failure.IsValid);
        Assert.Equal("user", failure.UserMessage);
        Assert.Null(failure.TechnicalMessage);
        Assert.Equal("technical", detailed.TechnicalMessage);
    }
}
