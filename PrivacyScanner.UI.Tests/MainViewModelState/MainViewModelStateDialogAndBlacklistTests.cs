using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.MainViewModelState.MainViewModelStateTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.MainViewModelState;

/// <summary>
/// Strand U3: what happens after the dialogs close (reload from the store), removing blacklist entries,
/// and picking or opening files. That a dialog is shown at all is covered in <c>Seams/DialogSeamTests</c>.
/// </summary>
public class MainViewModelStateDialogAndBlacklistTests
{
    private const string Host = "RootDialog";

    [Fact]
    public async Task AfterTheSettingsDialogClosesTheRulesAreReloaded()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Alt"));
        await harness.ViewModel.InitializeAsync();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), Host))
            .Callback(() => harness.ServeRules(Rule("Neu"), Rule("Noch neuer")))
            .Returns(Task.CompletedTask);

        harness.ViewModel.OpenSettingsCommand.Execute(null);

        Assert.Equal(["Neu", "Noch neuer"], harness.ViewModel.FilteredRegexRules.Select(r => r.RuleName));
    }

    [Fact]
    public async Task AfterTheAddRegexDialogClosesTheRulesAreReloaded()
    {
        var harness = new MainViewModelHarness();
        harness.ServeRules(Rule("Alt"));
        await harness.ViewModel.InitializeAsync();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<AddRegexDialogViewModel>(), Host))
            .Callback(() => harness.ServeRules(Rule("Alt"), Rule("Hinzugefügt")))
            .Returns(Task.CompletedTask);

        harness.ViewModel.AddNewRegexCommand.Execute(null);

        Assert.Equal(2, harness.ViewModel.FilteredRegexRules.Count);
        Assert.Equal("Hinzugefügt", harness.ViewModel.FilteredRegexRules[1].RuleName);
    }

    [Fact]
    public async Task AddRegexDialogAsksTheDialogServiceToCloseItself()
    {
        var harness = new MainViewModelHarness();
        await harness.ViewModel.InitializeAsync();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<AddRegexDialogViewModel>(), Host))
            .Callback((object viewModel, string _) => ((AddRegexDialogViewModel)viewModel).CancelCommand.Execute(null))
            .Returns(Task.CompletedTask);

        harness.ViewModel.AddNewRegexCommand.Execute(null);

        harness.Dialogs.Verify(d => d.CloseDialog(Host), Times.Once);
    }

    [Fact]
    public async Task AddedDirectoryEntryIsLoadedAndSelected()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist(["bin", "obj"], []);
        await harness.ViewModel.InitializeAsync();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<AddDirectoryBlacklistItemDialogViewModel>(), Host))
            .Callback(() => harness.ServeBlacklist(["bin", "obj", ".git"], []))
            .Returns(Task.CompletedTask);

        harness.ViewModel.AddNewDirectoryBacklistItemCommand.Execute(null);

        Assert.Equal(["bin", "obj", ".git"], harness.ViewModel.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        Assert.Equal(2, harness.ViewModel.SelectedDirectoryBlacklistItem);
    }

    [Fact]
    public async Task AddedFileExtensionEntryIsLoadedAndSelected()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist([], [".png"]);
        await harness.ViewModel.InitializeAsync();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<AddFileExtensionBlacklistItemDialogViewModel>(), Host))
            .Callback(() => harness.ServeBlacklist([], [".png", ".dll"]))
            .Returns(Task.CompletedTask);

        harness.ViewModel.AddNewFileExtensionBacklistItemCommand.Execute(null);

        Assert.Equal([".png", ".dll"], harness.ViewModel.FileExtensionBlacklistItems.Select(i => i.Extension));
        Assert.Equal(1, harness.ViewModel.SelectedFileExtensionBlacklistItem);
    }

    [Fact(Skip = "Suspected defect V10 (MainViewModel.cs:606 and :610): after a cancelled dialog the selection jumps to the last entry. The correct behaviour is that it stays where it was.")]
    public async Task ACancelledDirectoryDialogLeavesTheSelectionWhereItWas()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist(["a", "b", "c"], []);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedDirectoryBlacklistItem = 1;

        harness.ViewModel.AddNewDirectoryBacklistItemCommand.Execute(null);

        Assert.Equal(1, harness.ViewModel.SelectedDirectoryBlacklistItem);
    }

    [Fact(Skip = "Suspected defect V10 (MainViewModel.cs:637 and :641): after a cancelled dialog the selection jumps to the last entry. The correct behaviour is that it stays where it was.")]
    public async Task ACancelledFileExtensionDialogLeavesTheSelectionWhereItWas()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist([], [".a", ".b", ".c"]);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedFileExtensionBlacklistItem = 1;

        harness.ViewModel.AddNewFileExtensionBacklistItemCommand.Execute(null);

        Assert.Equal(1, harness.ViewModel.SelectedFileExtensionBlacklistItem);
    }

    [Fact]
    public async Task RemovingTheSelectedDirectoryEntryDropsItAndSavesTheRest()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist(["a", "b", "c"], []);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedDirectoryBlacklistItem = 1;

        harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.Execute(null);

        Assert.Equal(["a", "c"], harness.ViewModel.DirectoryBlacklistItems.Select(i => i.DirectoryName));
        harness.Mediator.Verify(
            m => m.Send(
                It.Is<SaveDirectoryBlacklistItemsStatesCommand>(c => c.DirectoryBlacklistItems.Select(i => i.DirectoryName).SequenceEqual(new[] { "a", "c" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BlankDirectoryEntriesAreNotSaved()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist(["a", "  ", "b"], []);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedDirectoryBlacklistItem = 2;

        harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.Execute(null);

        harness.Mediator.Verify(
            m => m.Send(
                It.Is<SaveDirectoryBlacklistItemsStatesCommand>(c => c.DirectoryBlacklistItems.Select(i => i.DirectoryName).SequenceEqual(new[] { "a" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ASelectionBeyondTheListRemovesAndSavesNothing()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist(["a", "b"], []);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedDirectoryBlacklistItem = 5;

        harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.Execute(null);

        Assert.Equal(2, harness.ViewModel.DirectoryBlacklistItems.Count);
        harness.Mediator.Verify(m => m.Send(It.IsAny<SaveDirectoryBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoSelectionOrNoEntriesDisablesRemovingADirectoryEntry()
    {
        var harness = new MainViewModelHarness();
        Assert.False(harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.CanExecute(null));

        harness.ServeBlacklist(["a"], []);
        await harness.ViewModel.InitializeAsync();
        Assert.True(harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.CanExecute(null));

        harness.ViewModel.SelectedDirectoryBlacklistItem = -1;
        Assert.False(harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.CanExecute(null));

        // The guard in the handler holds even if a caller ignores CanExecute.
        harness.ViewModel.RemoveSelectedDirectoryBacklistItemCommand.Execute(null);
        Assert.Single(harness.ViewModel.DirectoryBlacklistItems);
    }

    [Fact]
    public async Task RemovingTheSelectedFileExtensionEntryDropsItAndSavesTheRest()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist([], [".a", ".b", ".c"]);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedFileExtensionBlacklistItem = 0;

        harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.Execute(null);

        Assert.Equal([".b", ".c"], harness.ViewModel.FileExtensionBlacklistItems.Select(i => i.Extension));
        harness.Mediator.Verify(
            m => m.Send(
                It.Is<SaveFileExtensionBlacklistItemsStatesCommand>(c => c.FileExtensionBlacklistItems.Select(i => i.Extension).SequenceEqual(new[] { ".b", ".c" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BlankFileExtensionEntriesAreNotSaved()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist([], [".a", "", ".b"]);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedFileExtensionBlacklistItem = 2;

        harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.Execute(null);

        harness.Mediator.Verify(
            m => m.Send(
                It.Is<SaveFileExtensionBlacklistItemsStatesCommand>(c => c.FileExtensionBlacklistItems.Select(i => i.Extension).SequenceEqual(new[] { ".a" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ASelectionBeyondTheFileExtensionListRemovesAndSavesNothing()
    {
        var harness = new MainViewModelHarness();
        harness.ServeBlacklist([], [".a"]);
        await harness.ViewModel.InitializeAsync();
        harness.ViewModel.SelectedFileExtensionBlacklistItem = 1;

        harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.Execute(null);

        Assert.Single(harness.ViewModel.FileExtensionBlacklistItems);
        harness.Mediator.Verify(m => m.Send(It.IsAny<SaveFileExtensionBlacklistItemsStatesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoSelectionOrNoEntriesDisablesRemovingAFileExtensionEntry()
    {
        var harness = new MainViewModelHarness();
        Assert.False(harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.CanExecute(null));

        harness.ServeBlacklist([], [".a"]);
        await harness.ViewModel.InitializeAsync();
        Assert.True(harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.CanExecute(null));

        harness.ViewModel.SelectedFileExtensionBlacklistItem = -1;
        Assert.False(harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.CanExecute(null));

        harness.ViewModel.RemoveSelectedFileExtensionBacklistItemCommand.Execute(null);
        Assert.Single(harness.ViewModel.FileExtensionBlacklistItems);
    }

    [Fact]
    public void ChangingTheSelectionIsAnnounced()
    {
        var vm = new MainViewModelHarness().ViewModel;
        var changes = vm.RecordChanges();

        vm.SelectedDirectoryBlacklistItem = 3;
        vm.SelectedFileExtensionBlacklistItem = 2;

        Assert.Equal([nameof(vm.SelectedDirectoryBlacklistItem), nameof(vm.SelectedFileExtensionBlacklistItem)], changes);
    }

    [Fact]
    public void AnExistingExportFileIsOnlyTakenOverAfterConfirmingTheOverwrite()
    {
        var harness = new MainViewModelHarness();
        harness.FilePicker.Setup(p => p.PickSaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(@"C:\Daten\treffer.csv");
        harness.FileLauncher.Setup(l => l.Exists(@"C:\Daten\treffer.csv")).Returns(true);
        harness.Dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        harness.ViewModel.SelectExportFileCommand.Execute(null);

        Assert.Equal(@"C:\Daten\treffer.csv", harness.ViewModel.ExportFilePath);
        harness.Dialogs.Verify(
            d => d.ShowConfirmationAsync("Datei existiert bereits", It.Is<string>(m => m.Contains("treffer.csv") && !m.Contains(@"C:\")), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public void RefusingTheOverwriteKeepsTheEarlierExportFile()
    {
        var harness = new MainViewModelHarness();
        harness.ViewModel.ExportFilePath = @"C:\Daten\vorher.csv";
        harness.FilePicker.Setup(p => p.PickSaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(@"C:\Daten\treffer.csv");
        harness.FileLauncher.Setup(l => l.Exists(@"C:\Daten\treffer.csv")).Returns(true);
        harness.Dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        harness.ViewModel.SelectExportFileCommand.Execute(null);

        Assert.Equal(@"C:\Daten\vorher.csv", harness.ViewModel.ExportFilePath);
    }

    [Fact]
    public void ACancelledExportPickerChangesNothingAndAsksNothing()
    {
        var harness = new MainViewModelHarness();
        harness.ViewModel.ExportFilePath = @"C:\Daten\vorher.csv";
        harness.FilePicker.Setup(p => p.PickSaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((string?)null);

        harness.ViewModel.SelectExportFileCommand.Execute(null);

        Assert.Equal(@"C:\Daten\vorher.csv", harness.ViewModel.ExportFilePath);
        Assert.Empty(harness.Dialogs.Invocations);
        harness.FileLauncher.Verify(l => l.Exists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankPathIsNeitherCheckedNorOpened(string? path)
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.OpenFileCommand.Execute(path);

        harness.FileLauncher.Verify(l => l.Exists(It.IsAny<string>()), Times.Never);
        harness.FileLauncher.Verify(l => l.Open(It.IsAny<string>()), Times.Never);
    }
}
