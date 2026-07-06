using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Groups findings under a file node by detection type (Regex or SpaCy).</summary>
public class TypeLogNodeViewModel : LogTreeNodeViewModel
{
    public ScanWarningType Type { get; set; }
    public List<LogEntryViewModel> Warnings { get; set; } = new();
    public event Action<TypeLogNodeViewModel>? Expanded;

    protected override void OnExpanded()
    {
        if (!IsLoaded && !IsLoading)
        {
            Expanded?.Invoke(this);
        }
    }
}
