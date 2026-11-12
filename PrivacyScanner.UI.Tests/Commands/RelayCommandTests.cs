using ITTitans.PrivacyScanner.UI.Commands;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Commands;

/// <summary>
/// Covers the command type every button in the application is bound to.
/// </summary>
/// <remarks>
/// A fault here disables or enables controls across the whole window at once, and the
/// symptom is a button that does nothing rather than an error anyone would report.
/// </remarks>
public class RelayCommandTests
{
    [Fact]
    public void RunsTheActionItWasGiven()
    {
        var ran = false;
        var command = new RelayCommand(_ => ran = true);

        command.Execute(null);

        Assert.True(ran);
    }

    [Fact]
    public void PassesTheParameterThrough()
    {
        object? received = null;
        var command = new RelayCommand(parameter => received = parameter);

        command.Execute("rule-id");

        Assert.Equal("rule-id", received);
    }

    [Fact]
    public void IsExecutableWhenNoConditionWasSupplied()
    {
        // The common case: a button with no guard must not be dead.
        var command = new RelayCommand(_ => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void HonoursACondition()
    {
        var command = new RelayCommand(_ => { }, parameter => parameter is string);

        Assert.True(command.CanExecute("something"));
        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void PassesTheParameterToTheCondition()
    {
        object? received = null;
        var command = new RelayCommand(_ => { }, parameter =>
        {
            received = parameter;
            return true;
        });

        command.CanExecute(42);

        Assert.Equal(42, received);
    }

    [Fact]
    public void RefusesToBeConstructedWithoutAnAction()
    {
        // Without this guard the failure would surface as a NullReferenceException on
        // the click, far away from the line that built the command.
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }

    [Fact]
    public void RaisingCanExecuteChangedDoesNotThrowWithoutASubscriber()
    {
        var command = new RelayCommand(_ => { });

        var exception = Record.Exception(command.RaiseCanExecuteChanged);

        Assert.Null(exception);
    }

    [Fact]
    public void AcceptsAndReleasesASubscriber()
    {
        var command = new RelayCommand(_ => { });

        void Handler(object? sender, EventArgs args) { }

        var subscribe = Record.Exception(() => command.CanExecuteChanged += Handler);
        var unsubscribe = Record.Exception(() => command.CanExecuteChanged -= Handler);

        Assert.Null(subscribe);
        Assert.Null(unsubscribe);
    }
}
