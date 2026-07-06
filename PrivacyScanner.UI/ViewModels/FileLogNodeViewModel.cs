namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Top-level node in the results tree representing one scanned file.</summary>
public class FileLogNodeViewModel : LogTreeNodeViewModel
{
    public string FullPath { get; set; } = string.Empty;
    public event Action<FileLogNodeViewModel>? Expanded;

    protected override void OnExpanded()
    {
        if (!IsLoaded && !IsLoading)
        {
            Expanded?.Invoke(this);
        }
    }
}
