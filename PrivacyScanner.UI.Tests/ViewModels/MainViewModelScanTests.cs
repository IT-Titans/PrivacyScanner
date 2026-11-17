using System.IO;

using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.ViewModels.MainViewModelScanTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>
/// Covers the scan run of the main view model: start, stop, cancel, the end of the run and the two
/// notifications that report progress and findings.
/// </summary>
public class MainViewModelScanTests
{
    private const string Root = @"C:\Daten";

    private static TaskCompletionSource<Unit> HoldScan(MainViewModelHarness harness)
    {
        var gate = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<Unit>(gate.Task));

        return gate;
    }

    private static async Task<MainViewModelHarness> ReadyAsync()
    {
        var harness = new MainViewModelHarness();
        await LoadRulesAsync(harness, Rule("IBAN"));
        harness.ViewModel.SelectedPath = Root;

        return harness;
    }

    private static async Task FinishAsync(MainViewModelHarness harness, TaskCompletionSource<Unit> gate)
    {
        gate.SetResult(Unit.Value);
        await harness.ViewModel.ScanCompletion;
    }

    private static void ConfirmDialogs(MainViewModelHarness harness, bool answer) =>
        harness.Dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(answer);

    [Fact]
    public async Task StartingAScanShowsTheRunningState()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);

        harness.ViewModel.StartScanCommand.Execute(null);

        var vm = harness.ViewModel;
        Assert.True(vm.IsScanning);
        Assert.Equal("Scan läuft...", vm.StatusText);
        Assert.True(vm.IsProgressIndeterminate);
        Assert.Equal("Dateien werden gezählt...", vm.ProgressDisplayText);
        Assert.Equal("Scan stoppen", vm.ScanButtonText);
        Assert.Equal("Stop", vm.ScanButtonIcon);
        Assert.False(vm.IsConfigurationEnabled);
        Assert.False(vm.IsSpacyCheckboxEnabled);

        await FinishAsync(harness, gate);
    }

    [Fact]
    public async Task ARegularEndOfTheScanShowsCompletion()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        harness.ViewModel.StartScanCommand.Execute(null);

        await FinishAsync(harness, gate);

        var vm = harness.ViewModel;
        Assert.False(vm.IsScanning);
        Assert.False(vm.IsProgressIndeterminate);
        Assert.Equal("Scan abgeschlossen", vm.StatusText);
        Assert.Equal(100, vm.Progress);
        Assert.Equal("100%", vm.ProgressDisplayText);
        Assert.Equal("Scan starten", vm.ScanButtonText);
        Assert.Equal("Play", vm.ScanButtonIcon);
        Assert.True(vm.IsConfigurationEnabled);
    }

    [Fact]
    public async Task StartScanCommandNeedsAPath()
    {
        var harness = new MainViewModelHarness();
        await LoadRulesAsync(harness, Rule("IBAN"));

        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));

        harness.ViewModel.SelectedPath = Root;

        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartScanCommandNeedsAnEnabledRuleWhenSpacyIsOff()
    {
        var harness = new MainViewModelHarness();
        await LoadRulesAsync(harness, Rule("IBAN", enabled: false));
        harness.ViewModel.SelectedPath = Root;

        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ToggleScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartScanCommandAcceptsSpacyInPlaceOfARule()
    {
        var harness = new MainViewModelHarness();
        await LoadRulesAsync(harness, Rule("IBAN", enabled: false));
        harness.ViewModel.SelectedPath = Root;
        ConfirmDialogs(harness, true);

        harness.ViewModel.IsSpacyEnabled = true;

        Assert.True(harness.ViewModel.IsSpacyEnabled);
        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task CommandsAreBlockedWhileScanningAndFreeAgainAfterwards()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        harness.ViewModel.StartScanCommand.Execute(null);

        Assert.False(harness.ViewModel.StartScanCommand.CanExecute(null));
        Assert.True(harness.ViewModel.StopScanCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SelectPathCommand.CanExecute(null));
        Assert.False(harness.ViewModel.OpenSettingsCommand.CanExecute(null));
        Assert.False(harness.ViewModel.AddNewRegexCommand.CanExecute(null));

        await FinishAsync(harness, gate);

        Assert.True(harness.ViewModel.StartScanCommand.CanExecute(null));
        Assert.False(harness.ViewModel.StopScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task TogglingWhileIdleStartsTheScan()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);

        harness.ViewModel.ToggleScanCommand.Execute(null);

        Assert.True(harness.ViewModel.IsScanning);

        await FinishAsync(harness, gate);

        harness.Mediator.Verify(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TheStartCommandCarriesOnlyEnabledRulesAndTheBlacklists()
    {
        var harness = new MainViewModelHarness();
        var enabled = Rule("IBAN");
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<GetAllBlacklistItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GetAllBlacklistItemsQueryResult>(
                new GetAllBlacklistItemsQueryResult
                {
                    DirectoryBlacklistItems = [new DirectoryBlacklistItemDto { DirectoryName = "bin" }],
                    FileExtensionBlacklistItems = [new FileExtensionBlacklistItemDto { Extension = ".dll" }]
                }));
        await LoadRulesAsync(harness, enabled, Rule("Phone", enabled: false));
        harness.ViewModel.SelectedPath = Root;
        StartScannerCommand? sent = null;
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((c, _) => sent = (StartScannerCommand)c)
            .Returns(() => new ValueTask<Unit>(Unit.Value));

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        Assert.NotNull(sent);
        Assert.Equal(Root, sent.RootDirectory.FullName);
        var rule = Assert.Single(sent.RegexRuleList!);
        Assert.Equal(enabled.RuleId, rule.RuleId);
        Assert.Equal("IBAN", rule.RuleName);
        Assert.Equal("IBAN-pattern", rule.Rule);
        Assert.Equal("bin", Assert.Single(sent.DirectoryBlacklistItems).DirectoryName);
        Assert.Equal(".dll", Assert.Single(sent.FileExtensionBlacklistItems).Extension);
        Assert.False(sent.UseSpacy);
    }

    [Fact]
    public async Task TheStartCommandPassesTheSpacySwitch()
    {
        var harness = await ReadyAsync();
        ConfirmDialogs(harness, true);
        harness.ViewModel.IsSpacyEnabled = true;

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        harness.Mediator.Verify(
            m => m.Send(It.Is<StartScannerCommand>(c => c.UseSpacy), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AFreshScanClearsTheResultsOfTheLastOne()
    {
        var harness = await ReadyAsync();
        var vm = harness.ViewModel;
        await vm.Handle(Found(@"C:\Daten\a.txt", 2), default);
        await vm.Handle(new FileProcessedEvent { ScannedFilesCount = 3, TotalFilesCount = 4, ProgressInPercent = 75 }, default);
        Assert.Single(vm.PagedLogTreeNodes);
        var gate = HoldScan(harness);

        vm.StartScanCommand.Execute(null);

        Assert.Empty(vm.PagedLogTreeNodes);
        Assert.Equal(0, vm.TotalWarningsCount);
        Assert.Equal(0, vm.RegexWarningsCount);
        Assert.Equal(0, vm.SpacyWarningsCount);
        Assert.Equal(0, vm.ScannedFilesCount);
        Assert.Equal(0, vm.TotalFilesCount);
        Assert.Equal(0, vm.Progress);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, vm.TotalPages);

        await FinishAsync(harness, gate);
    }

    [Fact]
    public async Task AnExportPathInitialisesTheExportBeforeTheScanAndClosesItAfter()
    {
        var harness = await ReadyAsync();
        harness.ViewModel.ExportFilePath = @"C:\Out\result.csv";
        var gate = HoldScan(harness);

        harness.ViewModel.StartScanCommand.Execute(null);

        harness.CsvExport.Verify(c => c.InitializeExport(@"C:\Out\result.csv"), Times.Once);
        harness.CsvExport.Verify(c => c.Close(), Times.Never);

        await FinishAsync(harness, gate);

        harness.CsvExport.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task WithoutAnExportPathNoExportIsInitialised()
    {
        var harness = await ReadyAsync();

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        harness.CsvExport.Verify(c => c.InitializeExport(It.IsAny<string>()), Times.Never);
    }

    private const string ExportErrorText =
        "Der Scan wurde nicht gestartet, weil die Export-Datei nicht vorbereitet werden konnte: locked\n\n" +
        "Bitte korrigieren Sie den Pfad oder entfernen Sie die Export-Datei und starten Sie den Scan erneut.";

    private static async Task<MainViewModelHarness> WithFailedExportStartAsync()
    {
        var harness = await ReadyAsync();
        harness.ViewModel.ExportFilePath = @"C:\Out\locked.csv";
        harness.CsvExport.Setup(c => c.InitializeExport(@"C:\Out\locked.csv")).Throws(new IOException("locked"));

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        return harness;
    }

    [Fact]
    public async Task AFailingExportInitialisationDoesNotStartTheScan()
    {
        var harness = await WithFailedExportStartAsync();

        harness.Mediator.Verify(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AFailingExportInitialisationShowsTheReasonOnce()
    {
        var harness = await WithFailedExportStartAsync();

        harness.Dialogs.Verify(d => d.ShowError(ExportErrorText, "Fehler"), Times.Once);
        harness.Dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AFailingExportInitialisationLeavesTheUiIdleAndReleasesTheExport()
    {
        var harness = await WithFailedExportStartAsync();

        var vm = harness.ViewModel;
        Assert.False(vm.IsScanning);
        Assert.False(vm.IsProgressIndeterminate);
        Assert.Equal(0, vm.Progress);
        Assert.Equal("Scan nicht gestartet", vm.StatusText);
        Assert.Equal("Scan starten", vm.ScanButtonText);
        Assert.Equal("Play", vm.ScanButtonIcon);
        Assert.True(vm.IsConfigurationEnabled);
        Assert.True(vm.StartScanCommand.CanExecute(null));
        harness.CsvExport.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task AScanStartsAfterTheExportFileWasCleared()
    {
        var harness = await WithFailedExportStartAsync();

        harness.ViewModel.ClearExportFileCommand.Execute(null);
        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        harness.Mediator.Verify(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.CsvExport.Verify(c => c.InitializeExport(It.IsAny<string>()), Times.Once);
        Assert.Equal("Scan abgeschlossen", harness.ViewModel.StatusText);
    }

    [Fact]
    public async Task AScanStartsAfterTheExportPathWasCorrected()
    {
        var harness = await WithFailedExportStartAsync();

        harness.ViewModel.ExportFilePath = @"C:\Out\free.csv";
        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        harness.CsvExport.Verify(c => c.InitializeExport(@"C:\Out\free.csv"), Times.Once);
        harness.Mediator.Verify(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.Dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        Assert.False(harness.ViewModel.IsScanning);
    }

    [Fact]
    public async Task AFailingScanStillEndsTheRunAndClosesTheExport()
    {
        var harness = await ReadyAsync();
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<Unit>(new InvalidOperationException("boom")));

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        Assert.False(harness.ViewModel.IsScanning);
        Assert.False(harness.ViewModel.IsProgressIndeterminate);
        harness.CsvExport.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task ACancelledScanStillEndsTheRunAndClosesTheExport()
    {
        // Only the end state is asserted. How the cancellation is logged is being changed elsewhere.
        var harness = await ReadyAsync();
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<Unit>(new OperationCanceledException()));

        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        Assert.False(harness.ViewModel.IsScanning);
        harness.CsvExport.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task StopScanCommandSendsTheStopCommand()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        harness.ViewModel.StartScanCommand.Execute(null);

        harness.ViewModel.StopScanCommand.Execute(null);

        harness.Mediator.Verify(m => m.Send(It.IsAny<StopScannerCommand>(), It.IsAny<CancellationToken>()), Times.Once);

        await FinishAsync(harness, gate);
    }

    [Fact]
    public async Task ConfirmingTheCancelDialogStopsTheScanAndReportsAnAbort()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        ConfirmDialogs(harness, true);
        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.Handle(
            new FileProcessedEvent { ScannedFilesCount = 2, TotalFilesCount = 5, ProgressInPercent = 40 }, default);

        harness.ViewModel.ToggleScanCommand.Execute(null);
        await FinishAsync(harness, gate);

        harness.Mediator.Verify(m => m.Send(It.IsAny<StopScannerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Scan abgebrochen", harness.ViewModel.StatusText);
        Assert.Equal(40, harness.ViewModel.Progress);
        Assert.False(harness.ViewModel.IsScanning);
    }

    [Fact]
    public async Task DecliningTheCancelDialogLeavesTheScanRunning()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        ConfirmDialogs(harness, false);
        harness.ViewModel.StartScanCommand.Execute(null);

        harness.ViewModel.ToggleScanCommand.Execute(null);

        harness.Mediator.Verify(m => m.Send(It.IsAny<StopScannerCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(harness.ViewModel.IsScanning);

        await FinishAsync(harness, gate);

        Assert.Equal("Scan abgeschlossen", harness.ViewModel.StatusText);
    }

    [Fact]
    public async Task ANewScanAfterAnAbortReportsCompletionAgain()
    {
        var harness = await ReadyAsync();
        var gate = HoldScan(harness);
        ConfirmDialogs(harness, true);
        harness.ViewModel.StartScanCommand.Execute(null);
        harness.ViewModel.ToggleScanCommand.Execute(null);
        await FinishAsync(harness, gate);
        Assert.Equal("Scan abgebrochen", harness.ViewModel.StatusText);

        harness.Mediator
            .Setup(m => m.Send(It.IsAny<StartScannerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<Unit>(Unit.Value));
        harness.ViewModel.StartScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        Assert.Equal("Scan abgeschlossen", harness.ViewModel.StatusText);
        Assert.Equal(100, harness.ViewModel.Progress);
    }

    [Fact]
    public void IsScanningRaisesTheDerivedPropertyNotificationsOnlyOnChange()
    {
        var harness = new MainViewModelHarness();
        var raised = new List<string?>();
        harness.ViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        harness.ViewModel.IsScanning = true;

        Assert.Contains(nameof(MainViewModel.IsScanning), raised);
        Assert.Contains(nameof(MainViewModel.ScanButtonText), raised);
        Assert.Contains(nameof(MainViewModel.ScanButtonIcon), raised);
        Assert.Contains(nameof(MainViewModel.IsConfigurationEnabled), raised);
        Assert.Contains(nameof(MainViewModel.IsSpacyCheckboxEnabled), raised);

        raised.Clear();
        harness.ViewModel.IsScanning = true;

        Assert.Empty(raised);
    }

    [Fact]
    public void ChangingIsScanningHandsTheCommandRefreshToTheDispatcher()
    {
        // CanExecuteChanged goes through the WPF CommandManager, which a test cannot observe. What is
        // observable is that the refresh is posted to the dispatcher and not run directly.
        var harness = new MainViewModelHarness();
        harness.Dispatcher.RunImmediately = false;

        harness.ViewModel.IsScanning = true;

        Assert.Equal(1, harness.Dispatcher.PendingCount);

        harness.Dispatcher.RunPending();

        Assert.Equal(0, harness.Dispatcher.PendingCount);
    }

    [Fact]
    public async Task ProgressEventsUpdateTheCountersAndLeaveTheIndeterminateMode()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        vm.IsProgressIndeterminate = true;

        await vm.Handle(new FileProcessedEvent { ScannedFilesCount = 3, TotalFilesCount = 12, ProgressInPercent = 25 }, default);

        Assert.False(vm.IsProgressIndeterminate);
        Assert.Equal(3, vm.ScannedFilesCount);
        Assert.Equal(12, vm.TotalFilesCount);
        Assert.Equal(25, vm.Progress);
        Assert.Equal("25%", vm.ProgressDisplayText);
    }

    [Fact]
    public async Task ProgressEventsWithoutAKnownTotalKeepTheIndeterminateMode()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        vm.IsProgressIndeterminate = true;

        await vm.Handle(new FileProcessedEvent { ScannedFilesCount = 1, TotalFilesCount = 0, ProgressInPercent = 0 }, default);

        Assert.True(vm.IsProgressIndeterminate);
        Assert.Equal(1, vm.ScannedFilesCount);
    }

    [Fact]
    public async Task ProgressEventsAreHandedToTheDispatcher()
    {
        var harness = new MainViewModelHarness();
        harness.Dispatcher.RunImmediately = false;

        await harness.ViewModel.Handle(new FileProcessedEvent { ScannedFilesCount = 1, TotalFilesCount = 2, ProgressInPercent = 50 }, default);

        Assert.Equal(0, harness.ViewModel.Progress);

        harness.Dispatcher.RunPending();

        Assert.Equal(50, harness.ViewModel.Progress);
    }

    [Fact]
    public async Task FindingsAreCountedByType()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;

        await vm.Handle(
            Found(
                @"C:\Daten\a.txt",
                Warning(ScanWarningType.Rule),
                Warning(ScanWarningType.Rule),
                Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Per)),
            default);
        await vm.Handle(Found(@"C:\Daten\b.txt", Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Loc)), default);

        Assert.Equal(4, vm.TotalWarningsCount);
        Assert.Equal(2, vm.RegexWarningsCount);
        Assert.Equal(2, vm.SpacyWarningsCount);
    }

    [Fact]
    public async Task FindingsAreHandedToTheDispatcher()
    {
        var harness = new MainViewModelHarness();
        harness.Dispatcher.RunImmediately = false;

        await harness.ViewModel.Handle(Found(@"C:\Daten\a.txt", 1), default);

        Assert.Equal(0, harness.ViewModel.TotalWarningsCount);
        Assert.Empty(harness.ViewModel.PagedLogTreeNodes);

        harness.Dispatcher.RunPending();

        Assert.Equal(1, harness.ViewModel.TotalWarningsCount);
        Assert.Single(harness.ViewModel.PagedLogTreeNodes);
    }

    [Fact]
    public async Task FindingsAreWrittenToAnActiveExport()
    {
        var harness = new MainViewModelHarness();
        harness.CsvExport.SetupGet(c => c.IsExportActive).Returns(true);
        var written = new List<LogEntryViewModel>();
        harness.CsvExport.Setup(c => c.WriteEntry(It.IsAny<LogEntryViewModel>())).Callback<LogEntryViewModel>(written.Add);

        await harness.ViewModel.Handle(Found(@"C:\Daten\a.txt", 2), default);

        Assert.Equal(2, written.Count);
        Assert.All(written, e => Assert.Equal(@"C:\Daten\a.txt", e.FilePath));
        Assert.Equal([1, 2], written.Select(e => e.Line));
        Assert.All(written, e => Assert.Equal("IBAN", e.RuleName));
        Assert.All(written, e => Assert.Equal(2, e.Position));
        Assert.All(written, e => Assert.Equal(5, e.End));
    }

    [Fact]
    public async Task FindingsAreNotWrittenWithoutAnActiveExport()
    {
        var harness = new MainViewModelHarness();
        harness.CsvExport.SetupGet(c => c.IsExportActive).Returns(false);

        await harness.ViewModel.Handle(Found(@"C:\Daten\a.txt", 2), default);

        harness.CsvExport.Verify(c => c.WriteEntry(It.IsAny<LogEntryViewModel>()), Times.Never);
    }

    [Fact]
    public async Task AFailingExportWriteDoesNotLoseTheFindingsOnScreen()
    {
        var harness = new MainViewModelHarness();
        harness.CsvExport.SetupGet(c => c.IsExportActive).Returns(true);
        harness.CsvExport.Setup(c => c.WriteEntry(It.IsAny<LogEntryViewModel>())).Throws(new IOException("disk full"));

        await harness.ViewModel.Handle(Found(@"C:\Daten\a.txt", 2), default);

        harness.CsvExport.Verify(c => c.WriteEntry(It.IsAny<LogEntryViewModel>()), Times.Exactly(2));
        Assert.Equal(2, harness.ViewModel.TotalWarningsCount);
        Assert.Single(harness.ViewModel.PagedLogTreeNodes);
    }
}
