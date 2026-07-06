namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Represents one blacklisted file extension shown in the main window's blacklist list.</summary>
public class FileExtensionBlacklistItemViewModel() : ViewModelBase
{
    public string Extension
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;
}
