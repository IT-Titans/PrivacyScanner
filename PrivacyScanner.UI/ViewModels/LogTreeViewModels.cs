using System.Collections.ObjectModel;
using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public abstract class LogTreeNodeViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private int _warningCount;
    private bool _isExpanded;

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
        set => SetProperty(ref _isExpanded, value);
    }

    public ObservableCollection<LogTreeNodeViewModel> Children { get; } = new();
}

public class FileLogNodeViewModel : LogTreeNodeViewModel
{
    public string FullPath { get; set; } = string.Empty;
}

public class TypeLogNodeViewModel : LogTreeNodeViewModel
{
    public ScanWarningType Type { get; set; }
}

public class GroupLogNodeViewModel : LogTreeNodeViewModel
{
    public string GroupName { get; set; } = string.Empty;
}

public class EntryLogNodeViewModel : LogTreeNodeViewModel
{
    public LogEntryViewModel Entry { get; set; } = null!;
}
