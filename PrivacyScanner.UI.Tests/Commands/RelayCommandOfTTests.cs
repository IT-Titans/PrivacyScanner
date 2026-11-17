using ITTitans.PrivacyScanner.UI.Commands;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Commands;

/// <summary>
/// Covers the typed command used for buttons that pass a bound item as parameter.
/// </summary>
public class RelayCommandOfTTests
{
    [Fact]
    public void NullExecute_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand<string>(null!));
    }

    [Fact]
    public void Execute_PassesTypedParameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(s => received = s);

        command.Execute("abc");

        Assert.Equal("abc", received);
    }

    [Fact]
    public void Execute_WrongParameterType_PassesDefault()
    {
        var called = false;
        var received = -1;
        var command = new RelayCommand<int>(i =>
        {
            called = true;
            received = i;
        });

        command.Execute("not an int");

        Assert.True(called);
        Assert.Equal(0, received);
    }

    [Fact]
    public void Execute_NullParameter_PassesNullForReferenceTypes()
    {
        string? received = "unset";
        var command = new RelayCommand<string>(s => received = s);

        command.Execute(null);

        Assert.Null(received);
    }

    [Fact]
    public void CanExecute_WithoutPredicate_IsAlwaysTrue()
    {
        var command = new RelayCommand<string>(_ => { });

        Assert.True(command.CanExecute(null));
        Assert.True(command.CanExecute("x"));
        Assert.True(command.CanExecute(5));
    }

    [Fact]
    public void CanExecute_UsesPredicateForMatchingType()
    {
        var command = new RelayCommand<string>(_ => { }, s => s == "ok");

        Assert.True(command.CanExecute("ok"));
        Assert.False(command.CanExecute("no"));
    }

    [Fact]
    public void CanExecute_WithPredicateAndWrongTypeOrNull_IsFalse()
    {
        var command = new RelayCommand<string>(_ => { }, _ => true);

        Assert.False(command.CanExecute(5));
        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void CanExecuteChanged_CanBeSubscribedAndUnsubscribed()
    {
        var command = new RelayCommand<string>(_ => { });
        EventHandler handler = (_, _) => { };

        command.CanExecuteChanged += handler;
        command.CanExecuteChanged -= handler;

        Assert.True(command.CanExecute(null));
    }
}
