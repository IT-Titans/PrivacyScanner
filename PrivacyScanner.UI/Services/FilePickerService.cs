using Microsoft.Win32;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary><see cref="IFilePickerService"/> backed by the Windows common dialogs.</summary>
public class FilePickerService : IFilePickerService
{
    public string? PickFolder()
    {
        var dialog = new OpenFolderDialog();

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? PickSaveFile(string filter, string defaultExtension, string title)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExtension,
            Title = title
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
