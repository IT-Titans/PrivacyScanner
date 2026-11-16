using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Seams;

/// <summary>
/// Proves that the main view model reaches the UI thread through the dispatcher it was given.
/// </summary>
/// <remarks>
/// Before the seam existed every one of these calls went to the static WPF application, which does not
/// exist in a test process, so none of this code could run under test at all.
/// </remarks>
public class UiDispatcherSeamTests
{
    private static FileProcessedEvent Progress(int scanned, int total, int percent) => new()
    {
        ScannedFilesCount = scanned,
        TotalFilesCount = total,
        ProgressInPercent = percent
    };

    [Fact]
    public async Task ProgressNotificationIsAppliedWithoutAWpfApplication()
    {
        var harness = new MainViewModelHarness();

        await harness.ViewModel.Handle(Progress(3, 10, 30), CancellationToken.None);

        Assert.Equal(3, harness.ViewModel.ScannedFilesCount);
        Assert.Equal(10, harness.ViewModel.TotalFilesCount);
        Assert.Equal(30, harness.ViewModel.Progress);
    }

    [Fact]
    public async Task ProgressNotificationWaitsForTheDispatcher()
    {
        var harness = new MainViewModelHarness();
        harness.Dispatcher.RunImmediately = false;

        await harness.ViewModel.Handle(Progress(3, 10, 30), CancellationToken.None);

        // Handed over, not applied: the notification arrives on the scan's thread.
        Assert.Equal(1, harness.Dispatcher.PendingCount);
        Assert.Equal(0, harness.ViewModel.ScannedFilesCount);

        harness.Dispatcher.RunPending();

        Assert.Equal(3, harness.ViewModel.ScannedFilesCount);
    }

    [Fact]
    public async Task LoadingTheSettingsRunsWithoutAWpfApplication()
    {
        var harness = new MainViewModelHarness();

        var exception = await Record.ExceptionAsync(harness.ViewModel.InitializeAsync);

        Assert.Null(exception);
        Assert.False(harness.ViewModel.IsSpacyCheckboxEnabled);
    }
}
