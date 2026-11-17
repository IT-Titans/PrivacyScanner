using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Proves that the main view model asks the dialog service to show and close dialogs, and no longer
/// builds controls or calls the dialog host itself.
/// </summary>
/// <remarks>
/// The same seam in the settings dialog is covered where the rest of that view model is tested, in
/// <c>DialogViewModels/SettingsDialogViewModelTests</c>.
/// </remarks>
public class DialogSeamTests
{
    [Fact]
    public void OpeningTheSettingsShowsTheSettingsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.OpenSettingsCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingARegexShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewRegexCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddRegexDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingADirectoryBlacklistItemShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewDirectoryBacklistItemCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddDirectoryBlacklistItemDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void AddingAFileExtensionBlacklistItemShowsItsViewModelInTheRootDialog()
    {
        var harness = new MainViewModelHarness();

        harness.ViewModel.AddNewFileExtensionBacklistItemCommand.Execute(null);

        harness.Dialogs.Verify(d => d.ShowDialogAsync(It.IsAny<AddFileExtensionBlacklistItemDialogViewModel>(), "RootDialog"), Times.Once);
    }

    [Fact]
    public void ADialogThatAsksToCloseIsClosedThroughTheDialogService()
    {
        var harness = new MainViewModelHarness();
        harness.Dialogs
            .Setup(d => d.ShowDialogAsync(It.IsAny<SettingsDialogViewModel>(), "RootDialog"))
            .Callback((object viewModel, string _) => ((SettingsDialogViewModel)viewModel).CloseCommand.Execute(null))
            .Returns(Task.CompletedTask);

        harness.ViewModel.OpenSettingsCommand.Execute(null);

        harness.Dialogs.Verify(d => d.CloseDialog("RootDialog"), Times.Once);
    }
}
