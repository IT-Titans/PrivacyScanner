using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>Verifies what the three save handlers write: file name, content, overwrite and cancellation behaviour.</summary>
public sealed class SaveSettingsHandlersI3Tests : IDisposable
{
    private readonly SettingsHandlerTestDirectory _settings = new();

    public void Dispose() => _settings.Dispose();

    [Fact]
    public async Task SaveRegexRules_WritesAllRulesIncludingTheEnabledFlag()
    {
        var enabled = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Kundennummer", Rule = @"K\d{6}" };
        var disabled = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Alt", Rule = "alt", IsEnabled = false };

        await new SaveRegexRulesCommandHandler(_settings.Provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [enabled, disabled] }, CancellationToken.None);

        var saved = SettingsHandlerMediator.ReadList<RegexRuleDto>(_settings.RulesFile);
        Assert.Equal(2, saved.Count);
        Assert.Equal(enabled.RuleId, saved[0].RuleId);
        Assert.Equal(@"K\d{6}", saved[0].Rule);
        Assert.True(saved[0].IsEnabled);
        Assert.Equal("Alt", saved[1].RuleName);
        Assert.False(saved[1].IsEnabled);
    }

    [Fact]
    public async Task SaveRegexRules_ReplacesTheFileInsteadOfAppending()
    {
        var old = new[]
        {
            new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "alt", Rule = "x" },
            new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "alt2", Rule = "y" },
        };
        _settings.WriteFile(_settings.RulesFile, JsonSerializer.Serialize(old));
        var rule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "neu", Rule = "n" };

        await new SaveRegexRulesCommandHandler(_settings.Provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [rule] }, CancellationToken.None);

        var saved = Assert.Single(SettingsHandlerMediator.ReadList<RegexRuleDto>(_settings.RulesFile));
        Assert.Equal("neu", saved.RuleName);
    }

    [Fact]
    public async Task SaveRegexRules_EmptyList_WritesAnEmptyJsonArray()
    {
        await new SaveRegexRulesCommandHandler(_settings.Provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [] }, CancellationToken.None);

        Assert.Empty(SettingsHandlerMediator.ReadList<RegexRuleDto>(_settings.RulesFile));
    }

    [Fact]
    public async Task SaveRegexRules_WritesIndentedJson()
    {
        await new SaveRegexRulesCommandHandler(_settings.Provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "a", Rule = "b" }] },
            CancellationToken.None);

        Assert.Contains(Environment.NewLine, await File.ReadAllTextAsync(_settings.RulesFile));
    }

    [Fact]
    public async Task SaveRegexRules_CreatesMissingParentDirectories()
    {
        var nested = Path.Combine(_settings.Path, "a", "b");
        var provider = Mock.Of<ISettingsPathProvider>(p => p.GetSettingsDirectory() == nested);

        await new SaveRegexRulesCommandHandler(provider).Handle(
            new SaveRegexRulesCommand { RegexRuleDtos = [] }, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(nested, "rules.json")));
    }

    [Fact]
    public async Task SaveDirectoryBlacklist_WritesItemsToDirectoryBlacklistsJsonOnly()
    {
        await new SaveDirectoryBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
            new SaveDirectoryBlacklistItemsStatesCommand
            {
                DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }, new DirectoryBlacklistItemDto { DirectoryName = "obj" }]
            },
            CancellationToken.None);

        var saved = SettingsHandlerMediator.ReadList<DirectoryBlacklistItemDto>(_settings.DirectoryBlacklistFile);
        Assert.Equal(["bin", "obj"], saved.Select(i => i.DirectoryName));
        Assert.False(File.Exists(_settings.FileExtensionBlacklistFile));
        Assert.False(File.Exists(_settings.RulesFile));
    }

    [Fact]
    public async Task SaveDirectoryBlacklist_ReplacesTheFileAndKeepsEmptyList()
    {
        var sut = new SaveDirectoryBlacklistItemsStatesCommandHandler(_settings.Provider);
        await sut.Handle(
            new SaveDirectoryBlacklistItemsStatesCommand { DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }] },
            CancellationToken.None);

        await sut.Handle(new SaveDirectoryBlacklistItemsStatesCommand { DirectoryBlacklistItems = [] }, CancellationToken.None);

        Assert.Empty(SettingsHandlerMediator.ReadList<DirectoryBlacklistItemDto>(_settings.DirectoryBlacklistFile));
    }

    [Fact]
    public async Task SaveDirectoryBlacklist_CancelledToken_ThrowsAndWritesNothing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SaveDirectoryBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
                new SaveDirectoryBlacklistItemsStatesCommand { DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }] },
                cts.Token).AsTask());

        Assert.False(File.Exists(_settings.DirectoryBlacklistFile));
    }

    [Fact]
    public async Task SaveFileExtensionBlacklist_WritesItemsToFileExtensionBlacklistsJsonOnly()
    {
        await new SaveFileExtensionBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
            new SaveFileExtensionBlacklistItemsStatesCommand
            {
                FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".dll" }, new FileExtensionBlacklistItemDto { Extension = ".exe" }]
            },
            CancellationToken.None);

        var saved = SettingsHandlerMediator.ReadList<FileExtensionBlacklistItemDto>(_settings.FileExtensionBlacklistFile);
        Assert.Equal([".dll", ".exe"], saved.Select(i => i.Extension));
        Assert.False(File.Exists(_settings.DirectoryBlacklistFile));
    }

    [Fact]
    public async Task SaveFileExtensionBlacklist_ReplacesTheFileAndKeepsEmptyList()
    {
        var sut = new SaveFileExtensionBlacklistItemsStatesCommandHandler(_settings.Provider);
        await sut.Handle(
            new SaveFileExtensionBlacklistItemsStatesCommand { FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".dll" }] },
            CancellationToken.None);

        await sut.Handle(new SaveFileExtensionBlacklistItemsStatesCommand { FileExtensionBlacklistItems = [] }, CancellationToken.None);

        Assert.Equal("[]", (await File.ReadAllTextAsync(_settings.FileExtensionBlacklistFile)).Trim());
    }

    [Fact]
    public async Task SaveFileExtensionBlacklist_CancelledToken_ThrowsAndWritesNothing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SaveFileExtensionBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
                new SaveFileExtensionBlacklistItemsStatesCommand { FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".dll" }] },
                cts.Token).AsTask());

        Assert.False(File.Exists(_settings.FileExtensionBlacklistFile));
    }

    [Fact]
    public async Task SavedBlacklist_IsAPlainJsonArrayOfObjects()
    {
        await new SaveDirectoryBlacklistItemsStatesCommandHandler(_settings.Provider).Handle(
            new SaveDirectoryBlacklistItemsStatesCommand { DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }] },
            CancellationToken.None);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(_settings.DirectoryBlacklistFile));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal("bin", document.RootElement[0].GetProperty("DirectoryName").GetString());
    }
}
