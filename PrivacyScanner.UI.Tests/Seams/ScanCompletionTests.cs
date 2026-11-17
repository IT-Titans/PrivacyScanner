using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Moq;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Proves that a test can wait for the end of a scan. The scan runs on a background task that the view
/// model used to discard, which left a test nothing to await.
/// </summary>
public class ScanCompletionTests
{
    [Fact]
    public void NoScanMeansNothingToWaitFor()
    {
        var harness = new MainViewModelHarness();

        Assert.True(harness.ViewModel.ScanCompletion.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task AStartedScanCanBeAwaitedToItsEnd()
    {
        var harness = new MainViewModelHarness();
        harness.ViewModel.SelectedPath = @"C:\Daten";

        harness.ViewModel.ToggleScanCommand.Execute(null);
        await harness.ViewModel.ScanCompletion;

        harness.Mediator.Verify(
            m => m.Send(It.Is<StartScannerCommand>(c => c.RootDirectory.FullName == @"C:\Daten"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.False(harness.ViewModel.IsScanning);
        Assert.Equal("Scan abgeschlossen", harness.ViewModel.StatusText);
        Assert.Equal(100, harness.ViewModel.Progress);
        harness.CsvExport.Verify(c => c.Close(), Times.Once);
    }
}
