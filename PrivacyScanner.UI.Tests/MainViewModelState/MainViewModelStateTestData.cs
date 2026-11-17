using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Moq;

namespace ITTitans.PrivacyScanner.UI.Tests.MainViewModelState;

/// <summary>Arrange helpers shared by the state tests of <c>MainViewModel</c>.</summary>
internal static class MainViewModelStateTestData
{
    public static RegexRuleDto Rule(string name, bool enabled = true, string pattern = "x+") => new()
    {
        RuleId = Guid.NewGuid(),
        RuleName = name,
        Rule = pattern,
        IsEnabled = enabled
    };

    public static void ServeRules(this MainViewModelHarness harness, params RegexRuleDto[] rules) =>
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllRegexRulesQueryResult>(
                new GetAllRegexRulesQueryResult { Rules = rules.ToList() }));

    public static void ServeBlacklist(this MainViewModelHarness harness, string[] directories, string[] extensions) =>
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<GetAllBlacklistItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllBlacklistItemsQueryResult>(
                new GetAllBlacklistItemsQueryResult
                {
                    DirectoryBlacklistItems = directories.Select(d => new DirectoryBlacklistItemDto { DirectoryName = d }).ToList(),
                    FileExtensionBlacklistItems = extensions.Select(e => new FileExtensionBlacklistItemDto { Extension = e }).ToList()
                }));

    public static List<string> RecordChanges(this System.ComponentModel.INotifyPropertyChanged source)
    {
        var names = new List<string>();
        source.PropertyChanged += (_, e) => names.Add(e.PropertyName ?? string.Empty);

        return names;
    }
}
