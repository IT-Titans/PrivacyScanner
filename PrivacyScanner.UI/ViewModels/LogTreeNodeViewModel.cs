using System.Collections.ObjectModel;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Base type for lazily-expandable nodes in the scan results tree view.</summary>
public abstract class LogTreeNodeViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private int _warningCount;
    private bool _isExpanded;

    private bool _isLoaded;
    private bool _isLoading;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        set => SetProperty(ref _warningCount, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value)
            {
                OnExpanded();
            }
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetProperty(ref _isLoaded, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<LogTreeNodeViewModel> Children { get; } = new();

    protected virtual void OnExpanded() { }
}
