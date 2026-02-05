using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Models;

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

    public override bool Equals(object? obj)
    {
        if (obj is RegexRule other)
        {
            return RuleId == other.RuleId;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return RuleId.GetHashCode();
    }
}
