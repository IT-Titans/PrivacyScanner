using System.IO;

using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Moq;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>Builders shared by the scan and results-tree tests of the main view model.</summary>
internal static class MainViewModelScanTestData
{
    public static ScanWarningDto Warning(
        ScanWarningType type = ScanWarningType.Rule,
        string? ruleName = "IBAN",
        SpaCyLabel? label = null,
        int line = 1) => new()
        {
            Line = line,
            Start = 2,
            End = 5,
            SuspiciousContent = new SuspiciousContentDto { MatchText = "abc", PrevLine = null, HitLine = "xxabcxx", NextLine = null },
            Type = type,
            RuleName = ruleName,
            RuleId = null,
            SpacyLabel = label
        };

    public static FoundWarningEvent Found(string path, params ScanWarningDto[] warnings) => new()
    {
        ScanResultDto = new ScanResultDto { FilePath = new FileInfo(path), Warnings = warnings }
    };

    public static FoundWarningEvent Found(string path, int count) =>
        Found(path, Enumerable.Range(1, count).Select(i => Warning(line: i)).ToArray());

    public static RegexRuleDto Rule(string name, bool enabled = true) => new()
    {
        RuleId = Guid.NewGuid(),
        RuleName = name,
        Rule = name + "-pattern",
        IsEnabled = enabled
    };

    /// <summary>Makes the mediator return the given rules and runs the initial load.</summary>
    public static async Task LoadRulesAsync(MainViewModelHarness harness, params RegexRuleDto[] rules)
    {
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllRegexRulesQueryResult>(
                new GetAllRegexRulesQueryResult { Rules = rules.ToList() }));

        await harness.ViewModel.InitializeAsync();
    }
}
