namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Groups findings under a file node by rule name or spaCy label.</summary>
public class GroupLogNodeViewModel : LogTreeNodeViewModel
{
    public string GroupName { get; set; } = string.Empty;
    public List<LogEntryViewModel> Warnings { get; set; } = new();
    public event Action<GroupLogNodeViewModel>? Expanded;

    protected override void OnExpanded()
    {
        if (!IsLoaded && !IsLoading)
        {
            Expanded?.Invoke(this);
        }
    }
}
