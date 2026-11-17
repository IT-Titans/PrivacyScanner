using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>Verifies the two load queries: saved state wins, defaults fill the gaps, unreadable files fall back with a log entry.</summary>
public sealed class LoadSettingsQueriesI3Tests : IDisposable
{
    private readonly SettingsHandlerTestDirectory _settings = new();
    private readonly Mock<ILogger<GetAllBlacklistItemsQueryHandler>> _blacklistLogger = new();
    private readonly Mock<ILogger<GetAllRegexRulesQueryHandler>> _rulesLogger = new();

    public void Dispose() => _settings.Dispose();

    private GetAllBlacklistItemsQueryHandler BlacklistSut() => new(_blacklistLogger.Object, _settings.Provider);

    private GetAllRegexRulesQueryHandler RulesSut() => new(_rulesLogger.Object, _settings.Provider);

    private static string[] DefaultDirectories => DefaultBlacklistItemsProvider.GetDefaultDirectoryItems().Select(i => i.DirectoryName).ToArray();

    private static string[] DefaultExtensions => DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems().Select(i => i.Extension).ToArray();

    // Blacklists

    [Fact]
    public async Task Blacklists_NothingSaved_ReturnTheDefaultsAndDoNotCreateFiles()
    {
        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(DefaultDirectories, result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal(DefaultExtensions, result.FileExtensionBlacklistItems.Select(i => i.Extension));
        Assert.False(Directory.Exists(_settings.Path));
    }

    [Fact]
    public async Task Blacklists_SavedListReplacesTheDefaultsCompletely()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "[{\"DirectoryName\":\"archiv\"}]");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "[{\"Extension\":\".bak\"},{\"Extension\":\".tmp\"}]");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(["archiv"], result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal([".bak", ".tmp"], result.FileExtensionBlacklistItems.Select(i => i.Extension));
    }

    [Fact]
    public async Task Blacklists_OnlyTheDirectoryFileSaved_FileExtensionsStayAtDefaults()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "[{\"DirectoryName\":\"archiv\"}]");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(["archiv"], result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal(DefaultExtensions, result.FileExtensionBlacklistItems.Select(i => i.Extension));
    }

    [Fact]
    public async Task Blacklists_SavedEmptyArray_MeansNoEntriesAndNotDefaults()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "[]");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "[]");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Empty(result.DirectoryBlacklistItems);
        Assert.Empty(result.FileExtensionBlacklistItems);
    }

    [Fact]
    public async Task Blacklists_JsonNull_YieldsEmptyLists()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "null");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "null");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Empty(result.DirectoryBlacklistItems);
        Assert.Empty(result.FileExtensionBlacklistItems);
    }

    [Fact]
    public async Task Blacklists_CorruptDirectoryFile_FallsBackToDefaultsAndLogsAnError()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "{ not json");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "[{\"Extension\":\".bak\"}]");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(DefaultDirectories, result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal([".bak"], result.FileExtensionBlacklistItems.Select(i => i.Extension));
        _blacklistLogger.VerifyLogged(LogLevel.Error, "Verzeichnis-Blacklist", Times.Once());
        _blacklistLogger.VerifyLogged(LogLevel.Error, "Dateiendungs-Blacklist", Times.Never());
    }

    [Fact]
    public async Task Blacklists_FileExtensionFileWithMissingRequiredProperty_FallsBackToDefaultsAndLogsAnError()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "[{\"DirectoryName\":\"archiv\"}]");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "[{\"Wrong\":1}]");

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(["archiv"], result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal(DefaultExtensions, result.FileExtensionBlacklistItems.Select(i => i.Extension));
        _blacklistLogger.VerifyLogged(LogLevel.Error, "Dateiendungs-Blacklist", Times.Once());
    }

    [Fact]
    public async Task Blacklists_LockedFiles_FallBackToDefaultsAndLogAnError()
    {
        _settings.WriteFile(_settings.DirectoryBlacklistFile, "[{\"DirectoryName\":\"archiv\"}]");
        _settings.WriteFile(_settings.FileExtensionBlacklistFile, "[{\"Extension\":\".bak\"}]");
        using var lockDirectories = new FileStream(_settings.DirectoryBlacklistFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var lockExtensions = new FileStream(_settings.FileExtensionBlacklistFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal(DefaultDirectories, result.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal(DefaultExtensions, result.FileExtensionBlacklistItems.Select(i => i.Extension));
        _blacklistLogger.VerifyLogged(LogLevel.Error, "Verzeichnis-Blacklist", Times.Once());
        _blacklistLogger.VerifyLogged(LogLevel.Error, "Dateiendungs-Blacklist", Times.Once());
    }

    [Fact]
    public async Task Blacklists_SavedBySaveHandler_AreLoadedUnchangedIncludingNonAsciiNames()
    {
        await new SaveDirectoryBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
            new SaveDirectoryBlacklistItemsStatesCommand
            {
                DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "Ärzte" }]
            },
            CancellationToken.None);

        var result = await BlacklistSut().Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.Equal("Ärzte", Assert.Single(result.DirectoryBlacklistItems).DirectoryName);
    }

    // Regex rules

    [Fact]
    public async Task Rules_NothingSaved_ReturnExactlyTheDefaultRules()
    {
        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Select(r => r.RuleId), result.Rules.Select(r => r.RuleId));
        Assert.False(Directory.Exists(_settings.Path));
    }

    [Fact]
    public async Task Rules_UserRuleWithNewId_IsAppendedAfterTheDefaults()
    {
        var user = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Kundennummer", Rule = @"K\d{6}", IsEnabled = false };
        _settings.WriteFile(_settings.RulesFile, JsonSerializer.Serialize(new[] { user }));

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Count + 1, result.Rules.Count);
        var last = result.Rules[^1];
        Assert.Equal(user.RuleId, last.RuleId);
        Assert.Equal(@"K\d{6}", last.Rule);
        Assert.False(last.IsEnabled);
    }

    [Fact]
    public async Task Rules_UserRuleWithADefaultId_OverridesTheDefaultAndDoesNotDuplicateIt()
    {
        var defaults = DefaultRegexRulesProvider.GetDefaultRules();
        var overridden = defaults[0];
        var user = new RegexRuleDto { RuleId = overridden.RuleId, RuleName = "Angepasst", Rule = "angepasst", IsEnabled = false };
        _settings.WriteFile(_settings.RulesFile, JsonSerializer.Serialize(new[] { user }));

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(defaults.Count, result.Rules.Count);
        var match = Assert.Single(result.Rules, r => r.RuleId == overridden.RuleId);
        Assert.Equal("angepasst", match.Rule);
        Assert.False(match.IsEnabled);
        Assert.Equal(user.RuleId, result.Rules[^1].RuleId);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task Rules_EmptyOrNullFile_YieldsOnlyTheDefaultsWithoutAnError(string content)
    {
        _settings.WriteFile(_settings.RulesFile, content);

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Count, result.Rules.Count);
        _rulesLogger.VerifyLogged(LogLevel.Error, "Regex-Regeln", Times.Never());
    }

    [Fact]
    public async Task Rules_CorruptFile_YieldsOnlyTheDefaultsAndLogsAnError()
    {
        _settings.WriteFile(_settings.RulesFile, "[{ broken");

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Count, result.Rules.Count);
        _rulesLogger.VerifyLogged(LogLevel.Error, "Regex-Regeln", Times.Once());
    }

    [Fact]
    public async Task Rules_LockedFile_YieldsOnlyTheDefaultsAndLogsAnError()
    {
        _settings.WriteFile(_settings.RulesFile, "[]");
        using var locked = new FileStream(_settings.RulesFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Count, result.Rules.Count);
        _rulesLogger.VerifyLogged(LogLevel.Error, "Regex-Regeln", Times.Once());
    }

    [Fact]
    public async Task Rules_SavedBySaveHandler_AreLoadedBackWithAllFields()
    {
        var user = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Ärger", Rule = @"\bÄ+\b", IsEnabled = false };
        await new SaveRegexRulesCommandHandler(_settings.Provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [user] },
            CancellationToken.None);

        var result = await RulesSut().Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        var loaded = Assert.Single(result.Rules, r => r.RuleId == user.RuleId);
        Assert.Equal("Ärger", loaded.RuleName);
        Assert.Equal(@"\bÄ+\b", loaded.Rule);
        Assert.False(loaded.IsEnabled);
    }
}
