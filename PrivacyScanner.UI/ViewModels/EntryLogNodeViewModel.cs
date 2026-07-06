namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Leaf node in the results tree representing a single scan finding.</summary>
public class EntryLogNodeViewModel : LogTreeNodeViewModel
{
    public LogEntryViewModel Entry { get; set; } = null!;
}
