using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies that the regex scan reports each match with its position and the surrounding lines.</summary>
public class RegexScanCommandHandlerTests : IDisposable
{
    private readonly ScanEngineTempDirectory _temp = new();
    private readonly Mock<ILogger<ProcessScanCommandHandler>> _loggerMock = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Handle_MatchInMiddleLine_ReportsLinePositionAndContext()
    {
        var file = _temp.WriteText("a.txt", "alpha\nbeta 123 gamma\ndelta");

        var result = await Run(file, Rule("Digits", @"\d+"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(2, warning.Line);
        Assert.Equal(5, warning.Start);
        Assert.Equal(8, warning.End);
        Assert.Equal(ScanWarningType.Rule, warning.Type);
        Assert.Equal("Digits", warning.RuleName);
        Assert.Null(warning.SpacyLabel);
        Assert.Equal("123", warning.SuspiciousContent.MatchText);
        Assert.Equal("alpha", warning.SuspiciousContent.PrevLine);
        Assert.Equal("beta 123 gamma", warning.SuspiciousContent.HitLine);
        Assert.Equal("delta", warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_MatchInFirstLine_HasNoPreviousLine()
    {
        var file = _temp.WriteText("a.txt", "secret\nother");

        var result = await Run(file, Rule("Secret", "secret"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(1, warning.Line);
        Assert.Null(warning.SuspiciousContent.PrevLine);
        Assert.Equal("other", warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_MatchInLastLine_HasNoNextLine()
    {
        var file = _temp.WriteText("a.txt", "one\ntwo\nsecret");

        var result = await Run(file, Rule("Secret", "secret"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(3, warning.Line);
        Assert.Equal("two", warning.SuspiciousContent.PrevLine);
        Assert.Null(warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_SingleLineFile_HasNeitherPreviousNorNextLine()
    {
        var file = _temp.WriteText("a.txt", "only secret here");

        var result = await Run(file, Rule("Secret", "secret"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(1, warning.Line);
        Assert.Null(warning.SuspiciousContent.PrevLine);
        Assert.Null(warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_SeveralMatchesInOneLine_ReportsEachWithItsOwnPosition()
    {
        var file = _temp.WriteText("a.txt", "x 12 y 345");

        var result = await Run(file, Rule("Digits", @"\d+"));

        Assert.Collection(
            result.Warnings,
            w =>
            {
                Assert.Equal("12", w.SuspiciousContent.MatchText);
                Assert.Equal(2, w.Start);
                Assert.Equal(4, w.End);
            },
            w =>
            {
                Assert.Equal("345", w.SuspiciousContent.MatchText);
                Assert.Equal(7, w.Start);
                Assert.Equal(10, w.End);
            });
    }

    [Fact]
    public async Task Handle_TwoRulesHitSameLine_ReportsBothInRuleOrder()
    {
        var file = _temp.WriteText("a.txt", "mail a@b.de and 42");

        var result = await Run(file, Rule("Number", @"\d+"), Rule("Mail", @"[a-z]@[a-z]\.de"));

        Assert.Equal(["Number", "Mail"], result.Warnings.Select(w => w.RuleName));
    }

    [Fact]
    public async Task Handle_WarningsOfSeveralLines_AreOrderedByLine()
    {
        var file = _temp.WriteText("a.txt", "1\nno\n3\nno\n5");

        var result = await Run(file, Rule("Digit", @"\d"));

        Assert.Equal([1, 3, 5], result.Warnings.Select(w => w.Line));
    }

    [Fact]
    public async Task Handle_CrLfLineEndings_CountLinesTheSameWay()
    {
        var file = _temp.WriteText("a.txt", "a\r\nb\r\nsecret\r\nd");

        var result = await Run(file, Rule("Secret", "secret"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(3, warning.Line);
        Assert.Equal("b", warning.SuspiciousContent.PrevLine);
        Assert.Equal("d", warning.SuspiciousContent.NextLine);
    }

    [Fact]
    public async Task Handle_BlankLinesInFile_AreCountedAndCanMatch()
    {
        var file = _temp.WriteText("a.txt", "a\n\nb");

        var result = await Run(file, Rule("Empty", "^$"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal(2, warning.Line);
        Assert.Equal(0, warning.Start);
        Assert.Equal(0, warning.End);
        Assert.Equal(string.Empty, warning.SuspiciousContent.MatchText);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNoWarningsAndTheScannedFile()
    {
        var file = _temp.WriteText("a.txt", "nothing to see\nhere");

        var result = await Run(file, Rule("Digits", @"\d+"));

        Assert.Empty(result.Warnings);
        Assert.Same(file, result.FilePath);
    }

    [Fact]
    public async Task Handle_EmptyFile_ReturnsNoWarnings()
    {
        var file = _temp.WriteText("empty.txt", string.Empty);

        var result = await Run(file, Rule("Any", ".*"));

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_NoRules_ReturnsNoWarnings()
    {
        var file = _temp.WriteText("a.txt", "123");

        var result = await Run(file);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_MatchingIsCaseSensitive()
    {
        var file = _temp.WriteText("a.txt", "Secret");

        var result = await Run(file, Rule("Lower", "secret"));

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Handle_InvalidPattern_IsSkippedWithWarningAndOtherRulesStillRun()
    {
        var file = _temp.WriteText("a.txt", "value 7");

        var result = await Run(file, Rule("Broken", "(["), Rule("Digit", @"\d"));

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Digit", warning.RuleName);
        _loggerMock.VerifyLogged(LogLevel.Warning, "Broken", Times.Once());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_BlankPattern_IsTreatedAsInvalidAndSkipped(string pattern)
    {
        var file = _temp.WriteText("a.txt", "text");

        var result = await Run(file, Rule("Blank", pattern));

        Assert.Empty(result.Warnings);
        _loggerMock.VerifyLogged(LogLevel.Warning, "Blank", Times.Once());
    }

    [Fact]
    public async Task Handle_MissingFile_ThrowsIoException()
    {
        var file = new FileInfo(Path.Combine(_temp.Path, "missing.txt"));

        await Assert.ThrowsAnyAsync<IOException>(() => Run(file, Rule("Any", ".")));
    }

    [Fact]
    public async Task Handle_CancelledToken_ThrowsOperationCancelled()
    {
        var file = _temp.WriteText("a.txt", "a\nb\nc");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Run(file, cts.Token, Rule("Any", ".")));
    }

    private static RegexRuleDto Rule(string name, string pattern) =>
        new() { RuleName = name, Rule = pattern, RuleId = Guid.NewGuid() };

    private Task<ScanResultDto> Run(FileInfo file, params RegexRuleDto[] rules) =>
        Run(file, CancellationToken.None, rules);

    private async Task<ScanResultDto> Run(FileInfo file, CancellationToken token, params RegexRuleDto[] rules)
    {
        var sut = new RegexScanCommandHandler(_loggerMock.Object);

        return await sut.Handle(
            new RegexScanCommand { FilePath = file, RegexRuleList = [.. rules] },
            token);
    }
}
