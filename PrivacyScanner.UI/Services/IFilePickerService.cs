namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Lets the user pick a folder or a file to save to, abstracted away from ViewModels for testability.</summary>
public interface IFilePickerService
{
    /// <summary>Shows a folder picker. Returns the chosen folder, or <c>null</c> if the user cancelled.</summary>
    string? PickFolder();

    /// <summary>Shows a save-file picker. Returns the chosen file path, or <c>null</c> if the user cancelled.</summary>
    string? PickSaveFile(string filter, string defaultExtension, string title);
}
