using System.Collections.ObjectModel;
using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

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

public class LoadingLogNodeViewModel : LogTreeNodeViewModel { }

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

public class EntryLogNodeViewModel : LogTreeNodeViewModel
{
    public LogEntryViewModel Entry { get; set; } = null!;
}
