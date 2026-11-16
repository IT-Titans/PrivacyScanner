using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services;

/// <summary>
/// Verifies that the settings directory is still the one the application has always used, and that
/// the five handlers which read and write settings follow the provider instead of that fixed path.
/// </summary>
/// <remarks>
/// Without the provider these handlers could only be exercised against the real
/// <c>%ProgramData%\PrivacyScanner</c>, where a test would overwrite the settings of the machine it
/// runs on.
/// </remarks>
public sealed class SettingsPathProviderTests : IDisposable
{
    private readonly string _settingsDirectory = Path.Combine(Path.GetTempPath(), "PrivacyScannerTests_" + Guid.NewGuid().ToString("N"));
    private readonly ISettingsPathProvider _provider;

    public SettingsPathProviderTests()
    {
        _provider = Mock.Of<ISettingsPathProvider>(p => p.GetSettingsDirectory() == _settingsDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDirectory))
        {
            Directory.Delete(_settingsDirectory, recursive: true);
        }
    }

    [Fact]
    public void DefaultProvider_PointsAtProgramDataPrivacyScanner()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PrivacyScanner");

        Assert.Equal(expected, new SettingsPathProvider().GetSettingsDirectory());
    }

    [Fact]
    public async Task SaveRegexRules_CreatesTheProvidedDirectoryAndWritesRulesJsonIntoIt()
    {
        var rule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Kundennummer", Rule = @"K\d{6}" };

        await new SaveRegexRulesCommandHandler(_provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [rule] },
            CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_settingsDirectory, "rules.json")));
    }

    [Fact]
    public async Task GetAllRegexRules_ReadsUserRulesFromTheProvidedDirectory()
    {
        var userRule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Kundennummer", Rule = @"K\d{6}" };
        Directory.CreateDirectory(_settingsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(_settingsDirectory, "rules.json"),
            JsonSerializer.Serialize(new List<RegexRuleDto> { userRule }));
        var sut = new GetAllRegexRulesQueryHandler(Mock.Of<ILogger<GetAllRegexRulesQueryHandler>>(), _provider);

        var result = await sut.Handle(new GetAllRegexRulesQuery(), CancellationToken.None);

        Assert.Contains(result.Rules, r => r.RuleId == userRule.RuleId && r.Rule == userRule.Rule);
        Assert.Equal(DefaultRegexRulesProvider.GetDefaultRules().Count + 1, result.Rules.Count);
    }

    [Fact]
    public async Task Blacklists_AreWrittenToAndReadBackFromTheProvidedDirectory()
    {
        await new SaveDirectoryBlacklistItemsStatesCommandHandler(_provider).Handle(
            new SaveDirectoryBlacklistItemsStatesCommand
            {
                DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "archiv" }]
            },
            CancellationToken.None);
        await new SaveFileExtensionBlacklistItemsStatesCommandHandler(_provider).Handle(
            new SaveFileExtensionBlacklistItemsStatesCommand
            {
                FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".bak" }]
            },
            CancellationToken.None);
        var sut = new GetAllBlacklistItemsQueryHandler(Mock.Of<ILogger<GetAllBlacklistItemsQueryHandler>>(), _provider);

        var result = await sut.Handle(new GetAllBlacklistItemsQuery(), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_settingsDirectory, "directoryBlacklists.json")));
        Assert.True(File.Exists(Path.Combine(_settingsDirectory, "fileExtensionBlacklists.json")));
        Assert.Equal("archiv", Assert.Single(result.DirectoryBlacklistItems).DirectoryName);
        Assert.Equal(".bak", Assert.Single(result.FileExtensionBlacklistItems).Extension);
    }
}
