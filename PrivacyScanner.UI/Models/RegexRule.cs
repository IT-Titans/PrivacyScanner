using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Models;

/// <summary>UI-bindable regex rule (name, pattern, enabled state) shown in the rule chips and settings dialog.</summary>
public class RegexRule : ViewModelBase
{
    private bool _isEnabled = true;

    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    // Deliberately no Equals/GetHashCode override, so rules compare by reference. An edited rule is
    // reloaded as a new instance with the same id. With id equality WPF treats it as the same item and
    // keeps the old UI Automation peer, so assistive technology reads a stale or empty chip. No automated
    // test reproduces that peer state; it was verified against the running application with UI Automation.
    // Nothing looks rules up by Equals (the only Remove acts on an instance taken from the list).
}
