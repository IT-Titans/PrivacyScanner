namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Represents one blacklisted directory name shown in the main window's blacklist list.</summary>
public class DirectoryBlacklistItemViewModel() : ViewModelBase
{
    public string DirectoryName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;
}
