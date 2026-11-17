using ITTitans.PrivacyScanner.UI.ViewModels;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>
/// Covers the change-notification base class and the two blacklist list items built on it.
/// </summary>
public class ViewModelBaseAndBlacklistItemTests
{
    private sealed class Probe : ViewModelBase
    {
        private int _value;
        private string? _text;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string? Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public void Raise(string? name) => OnPropertyChanged(name);

        public bool SetExplicit(string v) => SetProperty(ref _text, v, "Custom");
    }

    [Fact]
    public void SetProperty_ChangedValue_StoresAndRaisesWithPropertyName()
    {
        var probe = new Probe();
        var seen = new List<(object? Sender, string? Name)>();
        probe.PropertyChanged += (s, e) => seen.Add((s, e.PropertyName));

        probe.Value = 5;

        Assert.Equal(5, probe.Value);
        var entry = Assert.Single(seen);
        Assert.Same(probe, entry.Sender);
        Assert.Equal("Value", entry.Name);
    }

    [Fact]
    public void SetProperty_EqualValue_RaisesNothing()
    {
        var probe = new Probe { Value = 5, Text = "a" };
        var count = 0;
        probe.PropertyChanged += (_, _) => count++;

        probe.Value = 5;
        probe.Text = "a";

        Assert.Equal(0, count);
    }

    [Fact]
    public void SetProperty_NullToValueAndBack_Raises()
    {
        var probe = new Probe();
        var count = 0;
        probe.PropertyChanged += (_, _) => count++;

        probe.Text = "x";
        probe.Text = null;
        probe.Text = null;

        Assert.Equal(2, count);
    }

    [Fact]
    public void SetProperty_ReturnsWhetherValueChangedAndHonoursExplicitName()
    {
        var probe = new Probe();
        string? name = null;
        probe.PropertyChanged += (_, e) => name = e.PropertyName;

        Assert.True(probe.SetExplicit("a"));
        Assert.Equal("Custom", name);
        Assert.False(probe.SetExplicit("a"));
    }

    [Fact]
    public void OnPropertyChanged_WithoutSubscriber_DoesNotThrow()
    {
        var probe = new Probe();

        probe.Raise("x");

        Assert.Equal(0, probe.Value);
    }

    [Fact]
    public void DirectoryItem_DefaultsToEmptyAndNotifiesOnChange()
    {
        var item = new DirectoryBlacklistItemViewModel();
        var names = new List<string?>();
        item.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        Assert.Equal(string.Empty, item.DirectoryName);
        item.DirectoryName = "node_modules";
        item.DirectoryName = "node_modules";

        Assert.Equal("node_modules", item.DirectoryName);
        Assert.Equal(new[] { "DirectoryName" }, names);
    }

    [Fact]
    public void ExtensionItem_DefaultsToEmptyAndNotifiesOnChange()
    {
        var item = new FileExtensionBlacklistItemViewModel();
        var names = new List<string?>();
        item.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        Assert.Equal(string.Empty, item.Extension);
        item.Extension = ".dll";
        item.Extension = ".dll";

        Assert.Equal(".dll", item.Extension);
        Assert.Equal(new[] { "Extension" }, names);
    }
}
