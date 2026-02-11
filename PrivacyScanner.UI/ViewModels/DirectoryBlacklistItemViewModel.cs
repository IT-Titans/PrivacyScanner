namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class DirectoryBlacklistItemViewModel() : ViewModelBase
{
    public string DirectoryName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;
}