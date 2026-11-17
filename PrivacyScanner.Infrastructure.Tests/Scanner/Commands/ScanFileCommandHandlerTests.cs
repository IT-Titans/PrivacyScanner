using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies how a single file is dispatched to the spaCy and regex scans and how their warnings are combined.</summary>
public class ScanFileCommandHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<ScanFileCommandHandler>> _loggerMock = new();
    private readonly FileInfo _file = new(Path.Combine(Path.GetTempPath(), "scan-file-target.txt"));

    [Fact]
    public async Task Handle_SpacyAndRegex_CombinesWarningsSpacyFirst()
    {
        SetupSpacy(Warning("spacy"));
        SetupRegex(Warning("regex"));

        var result = await Handle(useSpacy: true, rules: [Rule()]);

        Assert.Equal(["spacy", "regex"], result.Warnings.Select(w => w.SuspiciousContent.MatchText));
        Assert.Same(_file, result.FilePath);
    }

    [Fact]
    public async Task Handle_SpacyDisabled_DoesNotCallSpacy()
    {
        SetupRegex(Warning("regex"));

        var result = await Handle(useSpacy: false, rules: [Rule()]);

        Assert.Equal("regex", Assert.Single(result.Warnings).SuspiciousContent.MatchText);
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SpaCyScanCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_NoRegexRules_DoesNotCallRegexScan(bool emptyList)
    {
        SetupSpacy(Warning("spacy"));

        var result = await Handle(useSpacy: true, rules: emptyList ? [] : null);

        Assert.Equal("spacy", Assert.Single(result.Warnings).SuspiciousContent.MatchText);
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NeitherScanActive_ReturnsNoWarnings()
    {
        var result = await Handle(useSpacy: false, rules: null);

        Assert.Empty(result.Warnings);
        Assert.Same(_file, result.FilePath);
    }

    [Fact]
    public async Task Handle_RegexScan_ReceivesFileAndRules()
    {
        var rules = new List<RegexRuleDto> { Rule() };
        RegexScanCommand? sent = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ScanResultDto>, CancellationToken>((c, _) => sent = (RegexScanCommand)c)
            .ReturnsAsync(new ScanResultDto { FilePath = _file, Warnings = [] });

        await Handle(useSpacy: false, rules: rules);

        Assert.NotNull(sent);
        Assert.Same(_file, sent.FilePath);
        Assert.Same(rules, sent.RegexRuleList);
    }

    [Fact]
    public async Task Handle_SpacyThrows_LogsErrorAndStillRunsRegex()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SpaCyScanCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("python missing"));
        SetupRegex(Warning("regex"));

        var result = await Handle(useSpacy: true, rules: [Rule()]);

        Assert.Equal("regex", Assert.Single(result.Warnings).SuspiciousContent.MatchText);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RegexThrows_LogsErrorAndKeepsSpacyWarnings()
    {
        SetupSpacy(Warning("spacy"));
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new IOException("locked"));

        var result = await Handle(useSpacy: true, rules: [Rule()]);

        Assert.Equal("spacy", Assert.Single(result.Warnings).SuspiciousContent.MatchText);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(Skip = "Suspected defect from the analysis of 2026-11-06 (ScanFileCommandHandler.cs:27 and :43): catch (Exception) swallows OperationCanceledException, so a stop request during a file scan is logged as an error instead of ending the scan. The test states the correct behaviour; enable it once the behaviour is decided.")]
    public async Task Handle_Cancellation_IsNotSwallowed()
    {
        SetupSpacy(Warning("spacy"));
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Handle(useSpacy: true, rules: [Rule()]));
    }

    private Task<ScanResultDto> Handle(bool useSpacy, List<RegexRuleDto>? rules)
    {
        var sut = new ScanFileCommandHandler(_mediatorMock.Object, _loggerMock.Object);

        return sut.Handle(
            new ScanFileCommand { FilePath = _file, RegexRuleList = rules, UseSpacy = useSpacy },
            CancellationToken.None).AsTask();
    }

    private void SetupSpacy(params ScanWarningDto[] warnings) =>
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SpaCyScanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResultDto { FilePath = _file, Warnings = warnings });

    private void SetupRegex(params ScanWarningDto[] warnings) =>
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<RegexScanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScanResultDto { FilePath = _file, Warnings = warnings });

    private static RegexRuleDto Rule() =>
        new() { RuleName = "Rule", Rule = ".", RuleId = Guid.NewGuid() };

    private static ScanWarningDto Warning(string matchText) =>
        new()
        {
            Line = 1,
            Start = 0,
            End = matchText.Length,
            SuspiciousContent = new SuspiciousContentDto
            {
                MatchText = matchText,
                PrevLine = null,
                HitLine = matchText,
                NextLine = null
            },
            Type = ScanWarningType.Rule,
            RuleId = null,
            SpacyLabel = null
        };
}
