using ITTitans.PrivacyScanner.UI.Tests.TestSupport;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.MainViewModelState.MainViewModelStateTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.MainViewModelState;

/// <summary>Strand U3: switching spaCy on needs a confirmation, switching it off does not.</summary>
public class MainViewModelStateSpacyTests
{
    private static MainViewModelHarness HarnessAnswering(bool confirmed)
    {
        var harness = new MainViewModelHarness();
        harness.Dialogs
            .Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(confirmed);

        return harness;
    }

    [Fact]
    public void ConfirmingTheWarningEnablesSpacy()
    {
        var harness = HarnessAnswering(confirmed: true);
        var changes = harness.ViewModel.RecordChanges();

        harness.ViewModel.IsSpacyEnabled = true;

        Assert.True(harness.ViewModel.IsSpacyEnabled);
        Assert.Contains(nameof(harness.ViewModel.IsSpacyEnabled), changes);
        harness.Dialogs.Verify(
            d => d.ShowConfirmationAsync("SpaCy aktivieren", It.Is<string>(m => m.Contains("False-Positives")), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public void RefusingTheWarningLeavesSpacyOffButTellsTheCheckboxToUntick()
    {
        var harness = HarnessAnswering(confirmed: false);
        var changes = harness.ViewModel.RecordChanges();

        harness.ViewModel.IsSpacyEnabled = true;

        Assert.False(harness.ViewModel.IsSpacyEnabled);
        Assert.Contains(nameof(harness.ViewModel.IsSpacyEnabled), changes);
    }

    [Fact]
    public void SwitchingSpacyOffAsksNothingAndRequeriesTheScanCommands()
    {
        var harness = HarnessAnswering(confirmed: true);
        harness.ViewModel.IsSpacyEnabled = true;
        harness.Dialogs.Invocations.Clear();
        harness.Dispatcher.RunImmediately = false;

        harness.ViewModel.IsSpacyEnabled = false;

        Assert.False(harness.ViewModel.IsSpacyEnabled);
        Assert.Empty(harness.Dialogs.Invocations);
        Assert.Equal(1, harness.Dispatcher.PendingCount);
    }

    [Fact]
    public void SettingTheCurrentValueAgainDoesNothing()
    {
        var harness = HarnessAnswering(confirmed: true);
        var changes = harness.ViewModel.RecordChanges();

        harness.ViewModel.IsSpacyEnabled = false;

        Assert.Empty(harness.Dialogs.Invocations);
        Assert.Empty(changes);
    }
}
