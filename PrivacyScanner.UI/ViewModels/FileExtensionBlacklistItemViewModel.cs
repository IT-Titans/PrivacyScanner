namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class FileExtensionBlacklistItemViewModel() : ViewModelBase
{
    public string Extension
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;
}