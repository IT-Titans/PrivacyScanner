using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Proves that picking a folder, picking the export file and opening a hit go through the injected
/// services. Each of them used to open a Windows dialog or start a program directly.
/// </summary>
public class FilePickerAndLauncherSeamTests
{
    private const string ExportFilter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";

    [Fact]
    public void ThePickedFolderBecomesTheScanPath()
    {
        var harness = new MainViewModelHarness();
        harness.FilePicker.Setup(p => p.PickFolder()).Returns(@"C:\Daten");

        harness.ViewModel.SelectPathCommand.Execute(null);

        Assert.Equal(@"C:\Daten", harness.ViewModel.SelectedPath);
    }

    [Fact]
    public void ACancelledFolderPickerLeavesTheScanPathAlone()
    {
        var harness = new MainViewModelHarness();
        harness.ViewModel.SelectedPath = @"C:\Vorher";
        harness.FilePicker.Setup(p => p.PickFolder()).Returns((string?)null);

        harness.ViewModel.SelectPathCommand.Execute(null);

        Assert.Equal(@"C:\Vorher", harness.ViewModel.SelectedPath);
    }

    [Fact]
    public void ThePickedExportFileIsTakenOverWithTheSameDialogTexts()
    {
        var harness = new MainViewModelHarness();
        harness.FilePicker
            .Setup(p => p.PickSaveFile(ExportFilter, ".csv", "Export-Datei auswählen"))
            .Returns(@"C:\Daten\treffer.csv");
        harness.FileLauncher.Setup(l => l.Exists(@"C:\Daten\treffer.csv")).Returns(false);

        harness.ViewModel.SelectExportFileCommand.Execute(null);

        Assert.Equal(@"C:\Daten\treffer.csv", harness.ViewModel.ExportFilePath);
        harness.Dialogs.Verify(
            d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void AnExistingFileIsOpenedThroughTheLauncher()
    {
        var harness = new MainViewModelHarness();
        harness.FileLauncher.Setup(l => l.Exists(@"C:\Daten\brief.txt")).Returns(true);

        harness.ViewModel.OpenFileCommand.Execute(@"C:\Daten\brief.txt");

        harness.FileLauncher.Verify(l => l.Open(@"C:\Daten\brief.txt"), Times.Once);
    }

    [Fact]
    public void AMissingFileIsNotHandedToTheLauncher()
    {
        var harness = new MainViewModelHarness();
        harness.FileLauncher.Setup(l => l.Exists(It.IsAny<string>())).Returns(false);

        harness.ViewModel.OpenFileCommand.Execute(@"C:\Daten\weg.txt");

        harness.FileLauncher.Verify(l => l.Open(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ALauncherFailureDoesNotEscapeTheCommand()
    {
        var harness = new MainViewModelHarness();
        harness.FileLauncher.Setup(l => l.Exists(It.IsAny<string>())).Returns(true);
        harness.FileLauncher.Setup(l => l.Open(It.IsAny<string>())).Throws(new InvalidOperationException("no handler"));

        var exception = Record.Exception(() => harness.ViewModel.OpenFileCommand.Execute(@"C:\Daten\brief.xyz"));

        Assert.Null(exception);
    }
}
