using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.MainViewModelState.MainViewModelStateTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.MainViewModelState;

/// <summary>
/// Strand U3: construction, bound properties, the conditions that enable the commands, rule filtering and
/// toggling, and the start-up load. Scanning and the result tree belong to strand U4.
/// </summary>
public class MainViewModelStateTests
{
    [Theory]
    [InlineData(0, "mediator")]
    [InlineData(1, "logger")]
    [InlineData(2, "dialogService")]
    [InlineData(3, "csvExportService")]
    [InlineData(4, "uiDispatcher")]
    [InlineData(5, "filePickerService")]
    [InlineData(6, "fileLauncher")]
    public void EveryCollaboratorIsRequired(int nullIndex, string parameterName)
    {
        var harness = new MainViewModelHarness();
        object?[] args =
        [
            harness.Mediator.Object, NullLogger<MainViewModel>.Instance, harness.Dialogs.Object, harness.CsvExport.Object,
            harness.Dispatcher, harness.FilePicker.Object, harness.FileLauncher.Object
        ];
        args[nullIndex] = null;

        var exception = Assert.Throws<ArgumentNullException>(() => new MainViewModel(
            (IMediator)args[0]!,
            (ILogger<MainViewModel>)args[1]!,
            (IDialogService)args[2]!,
            (ICsvExportService)args[3]!,
            (IUiDispatcher)args[4]!,
            (IFilePickerService)args[5]!,
            (IFileLauncher)args[6]!));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void ANewViewModelStartsIdleAndEmpty()
    {
        var vm = new MainViewModelHarness().ViewModel;

        Assert.Equal("Bereit", vm.StatusText);
        Assert.Equal(string.Empty, vm.SelectedPath);
        Assert.Equal(string.Empty, vm.ExportFilePath);
        Assert.Equal(string.Empty, vm.RegexSearchText);
        Assert.False(vm.IsScanning);
        Assert.True(vm.IsConfigurationEnabled);
        Assert.False(vm.IsSpacyEnabled);
        Assert.Equal(0, vm.Progress);
        Assert.Equal(0, vm.TotalWarningsCount);
        Assert.Empty(vm.FilteredRegexRules);
        Assert.Empty(vm.DirectoryBlacklistItems);
        Assert.Empty(vm.FileExtensionBlacklistItems);
        Assert.Equal("Scan starten", vm.ScanButtonText);
        Assert.Equal("Play", vm.ScanButtonIcon);
    }

    [Fact]
    public void WhileIdleOnlyTheCommandsThatNeedNothingAreEnabled()
    {
        var vm = new MainViewModelHarness().ViewModel;

        Assert.True(vm.SelectPathCommand.CanExecute(null));
        Assert.True(vm.SelectExportFileCommand.CanExecute(null));
        Assert.True(vm.OpenSettingsCommand.CanExecute(null));
        Assert.True(vm.AddNewRegexCommand.CanExecute(null));
        Assert.True(vm.AddNewDirectoryBacklistItemCommand.CanExecute(null));
        Assert.True(vm.AddNewFileExtensionBacklistItemCommand.CanExecute(null));
        Assert.True(vm.ToggleRuleCommand.CanExecute(new RegexRule()));

        Assert.False(vm.ClearExportFileCommand.CanExecute(null));
        Assert.False(vm.ToggleScanCommand.CanExecute(null));
        Assert.False(vm.StartScanCommand.CanExecute(null));
        Assert.False(vm.StopScanCommand.CanExecute(null));
        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.PreviousPageCommand.CanExecute(null));
        Assert.False(vm.ToggleRuleCommand.CanExecute(null));
    }

    [Fact]
    public async Task ScanNeedsAPathAndAnEnabledRule()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email", enabled: false), Rule("IBAN", enabled: true));
        await harness.ViewModel.InitializeAsync();

        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));

        harness.ViewModel.SelectedPath = "   ";
        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));

        harness.ViewModel.SelectedPath = @"C:\Daten";
        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
        Assert.True(harness.ViewModel.ToggleScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task ScanWithoutAnyEnabledRuleIsRefusedUnlessSpacyIsOn()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email", enabled: false));
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedPath = @"C:\Daten";
        harness.Dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        Assert.False(harness.ViewModel.ToggleScanCommand.CanExecute(null));

        harness.ViewModel.IsSpacyEnabled = true;

        Assert.True(harness.ViewModel.ToggleScanCommand.CanExecute(null));
        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
    }

    [Fact]
    public void ChangingThePathAndTheExportFileIsAnnounced()
    {
        var vm = new MainViewModelHarness().ViewModel;
        var changes = vm.RecordChanges();

        vm.SelectedPath = @"C:\Daten";
        vm.ExportFilePath = @"C:\Daten\treffer.csv";
        vm.SelectedPath = @"C:\Daten";

        Assert.Equal([nameof(vm.SelectedPath), nameof(vm.ExportFilePath)], changes);
        Assert.True(vm.ClearExportFileCommand.CanExecute(null));
    }

    [Fact]
    public void ClearingTheExportFileEmptiesItAndDisablesTheCommand()
    {
        var vm = new MainViewModelHarness().ViewModel;
        vm.ExportFilePath = @"C:\Daten\treffer.csv";

        vm.ClearExportFileCommand.Execute(null);

        Assert.Equal(string.Empty, vm.ExportFilePath);
        Assert.False(vm.ClearExportFileCommand.CanExecute(null));
    }

    [Fact]
    public void StatusAndCountersAreAnnouncedWhenTheyChange()
    {
        var vm = new MainViewModelHarness().ViewModel;
        var changes = vm.RecordChanges();

        vm.StatusText = "Scan läuft...";
        vm.ScannedFilesCount = 4;
        vm.TotalFilesCount = 9;
        vm.TotalWarningsCount = 7;
        vm.RegexWarningsCount = 5;
        vm.SpacyWarningsCount = 2;

        Assert.Equal("Scan läuft...", vm.StatusText);
        Assert.Equal(4, vm.ScannedFilesCount);
        Assert.Equal(9, vm.TotalFilesCount);
        Assert.Equal(7, vm.TotalWarningsCount);
        Assert.Equal(5, vm.RegexWarningsCount);
        Assert.Equal(2, vm.SpacyWarningsCount);
        Assert.Equal(
            [
                nameof(vm.StatusText), nameof(vm.ScannedFilesCount), nameof(vm.TotalFilesCount),
                nameof(vm.TotalWarningsCount), nameof(vm.RegexWarningsCount), nameof(vm.SpacyWarningsCount)
            ],
            changes);
    }

    [Fact]
    public void ProgressIsShownAsAPercentageUntilTheTotalIsKnown()
    {
        var vm = new MainViewModelHarness().ViewModel;
        var changes = vm.RecordChanges();

        vm.Progress = 42;

        Assert.Equal("42%", vm.ProgressDisplayText);
        Assert.Contains(nameof(vm.ProgressDisplayText), changes);

        changes.Clear();
        vm.IsProgressIndeterminate = true;

        Assert.Equal("Dateien werden gezählt...", vm.ProgressDisplayText);
        Assert.Contains(nameof(vm.ProgressDisplayText), changes);
    }

    [Fact]
    public async Task WithoutPythonTheSpacyCheckboxIsDisabledAndSaysWhy()
    {
        var harness = new MainViewModelHarness();
        var changes = harness.ViewModel.RecordChanges();

        await harness.ViewModel.InitializeAsync();

        Assert.False(harness.ViewModel.IsSpacyCheckboxEnabled);
        Assert.Equal("SpaCy aktivieren (nicht verfügbar)", harness.ViewModel.SpacyCheckboxContent);
        Assert.Contains(nameof(MainViewModel.IsSpacyCheckboxEnabled), changes);
        Assert.Contains(nameof(MainViewModel.SpacyCheckboxContent), changes);
    }

    [Fact]
    public async Task WithPythonAndSpacyTheCheckboxIsEnabled()
    {
        var harness = new MainViewModelHarness();
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<CheckPythonAndSpacyEnvironmentQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<CheckPythonAndSpacyEnvironmentQueryResult>(CheckPythonAndSpacyEnvironmentQueryResult.Success()));

        await harness.ViewModel.InitializeAsync();

        Assert.True(harness.ViewModel.IsSpacyCheckboxEnabled);
        Assert.Equal("SpaCy aktivieren", harness.ViewModel.SpacyCheckboxContent);
    }

    [Fact]
    public async Task InitializeLoadsRulesAndBlacklistsIntoTheBoundCollections()
    {
        var harness = new MainViewModelHarness();
        var email = Rule("Email", enabled: true, pattern: @"\w+@\w+");
        var iban = Rule("IBAN", enabled: false, pattern: "DE\\d+");
        harness.ServeRules(email, iban);
        harness.ServeBlacklist(["node_modules", "bin"], [".png", ".dll"]);

        await harness.ViewModel.InitializeAsync();

        Assert.Collection(
            harness.ViewModel.FilteredRegexRules,
            r =>
            {
                Assert.Equal(email.RuleId, r.RuleId);
                Assert.Equal("Email", r.RuleName);
                Assert.Equal(@"\w+@\w+", r.Rule);
                Assert.True(r.IsEnabled);
            },
            r =>
            {
                Assert.Equal(iban.RuleId, r.RuleId);
                Assert.False(r.IsEnabled);
            });
        Assert.Equal(["node_modules", "bin"], harness.ViewModel.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal([".png", ".dll"], harness.ViewModel.FileExtensionBlacklistItems.Select(i => i.Extension));
    }

    [Fact]
    public async Task ReloadingReplacesTheListsInsteadOfAppendingToThem()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email"));
        harness.ServeBlacklist(["bin"], [".png"]);
        await harness.ViewModel.InitializeAsync();

        await harness.ViewModel.InitializeAsync();

        Assert.Single(harness.ViewModel.FilteredRegexRules);
        Assert.Single(harness.ViewModel.DirectoryBlacklistItems);
        Assert.Single(harness.ViewModel.FileExtensionBlacklistItems);
    }

    [Fact]
    public async Task LoadingHandsTheCommandRequeryToTheDispatcher()
    {
        var harness = new MainViewModelHarness();
        harness.Dispatcher.RunImmediately = false;

        await harness.ViewModel.InitializeAsync();

        // One requery after the blacklists, one after the rules.
        Assert.Equal(2, harness.Dispatcher.PendingCount);
    }

    [Fact]
    public async Task SearchTextFiltersRulesByNameIgnoringCase()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("E-Mail-Adresse"), Rule("IBAN"), Rule("Telefonnummer"));
        await harness.ViewModel.InitializeAsync();

        harness.ViewModel.RegexSearchText = "iBa";

        Assert.Equal(["IBAN"], harness.ViewModel.FilteredRegexRules.Select(r => r.RuleName));
    }

    [Fact]
    public async Task ClearingOrBlankingTheSearchTextShowsEveryRuleInTheLoadedOrder()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email"), Rule("IBAN"), Rule("Telefon"));
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.RegexSearchText = "zzz";
        Assert.Empty(harness.ViewModel.FilteredRegexRules);

        harness.ViewModel.RegexSearchText = "  ";
        Assert.Equal(["Email", "IBAN", "Telefon"], harness.ViewModel.FilteredRegexRules.Select(r => r.RuleName));

        harness.ViewModel.RegexSearchText = "e";
        harness.ViewModel.RegexSearchText = string.Empty;
        Assert.Equal(3, harness.ViewModel.FilteredRegexRules.Count);
    }

    [Fact]
    public async Task TogglingARuleFlipsItAndSavesTheStateOfAllRules()
    {
        var harness = new MainViewModelHarness();
        var email = Rule("Email", enabled: true);
        var iban = Rule("IBAN", enabled: false);
        harness.ServeRules(email, iban);
        await harness.ViewModel.InitializeAsync();
        var rule = harness.ViewModel.FilteredRegexRules[0];

        harness.ViewModel.ToggleRuleCommand.Execute(rule);

        Assert.False(rule.IsEnabled);
        harness.Mediator.Verify(
            m => m.Send(
                It.Is<SaveRegexRulesCommand>(c =>
                    c.RegexRuleDtos.Count == 2
                    && c.RegexRuleDtos[0].RuleId == email.RuleId && !c.RegexRuleDtos[0].IsEnabled
                    && c.RegexRuleDtos[0].RuleName == "Email" && c.RegexRuleDtos[0].Rule == "x+"
                    && c.RegexRuleDtos[1].RuleId == iban.RuleId && !c.RegexRuleDtos[1].IsEnabled),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TogglingTheLastEnabledRuleDisablesTheScan()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email", enabled: true));
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedPath = @"C:\Daten";
        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));

        harness.ViewModel.ToggleRuleCommand.Execute(harness.ViewModel.FilteredRegexRules[0]);

        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));

        harness.ViewModel.ToggleRuleCommand.Execute(harness.ViewModel.FilteredRegexRules[0]);

        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
    }

    [Fact]
    public void TogglingNothingSavesNothing()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.ToggleRuleCommand.Execute(null);

        harness.Mediator.Verify(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TogglingARuleRequeriesTheCommandsThroughTheDispatcher()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Email"));
        await harness.ViewModel.InitializeAsync();
        harness.Dispatcher.RunImmediately = false;

        harness.ViewModel.ToggleRuleCommand.Execute(harness.ViewModel.FilteredRegexRules[0]);

        Assert.Equal(1, harness.Dispatcher.PendingCount);
    }

    [Fact]
    public void AnUnusedPageSizeChangeKeepsTheViewOnPageOne()
    {
        var vm = new MainViewModelHarness().ViewModel;
        var changes = vm.RecordChanges();

        vm.PageSize = 25;

        Assert.Equal(25, vm.PageSize);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, vm.TotalPages);
        Assert.Contains(nameof(vm.PageSize), changes);
        Assert.Empty(vm.PagedLogTreeNodes);
    }

    [Fact]
    public void ThereIsNoPageBeyondTheLastOne()
    {
        var vm = new MainViewModelHarness().ViewModel;

        vm.CurrentPage = 3;

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, vm.TotalPages);
        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.PreviousPageCommand.CanExecute(null));
    }

    [Fact]
    public void ThereIsNoPageBeforeTheFirstOne()
    {
        var vm = new MainViewModelHarness().ViewModel;

        vm.CurrentPage = 0;

        Assert.Equal(1, vm.CurrentPage);
    }
}
