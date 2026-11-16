using System.Threading.Tasks;
using System.Windows;
using ITTitans.PrivacyScanner.UI.Controls;
using ITTitans.PrivacyScanner.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Shows Material Design dialogs via <see cref="DialogHost"/> and error messages via <see cref="MessageBox"/>.</summary>
public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message, string dialogIdentifier = "RootDialog")
    {
        var dialog = new ConfirmationDialog(title, message);
        var result = await DialogHost.Show(dialog, dialogIdentifier);

        return result is bool boolResult && boolResult;
    }

    public async Task ShowDialogAsync(object viewModel, string dialogIdentifier = "RootDialog")
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var dialog = CreateDialogFor(viewModel);
        dialog.DataContext = viewModel;

        await DialogHost.Show(dialog, dialogIdentifier);
    }

    public void CloseDialog(string dialogIdentifier = "RootDialog")
    {
        DialogHost.Close(dialogIdentifier);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// The one place that knows which control renders which dialog ViewModel. A ViewModel without an
    /// entry here is a programming error, not something to recover from.
    /// </summary>
    private static FrameworkElement CreateDialogFor(object viewModel) => viewModel switch
    {
        SettingsDialogViewModel => new SettingsDialog(),
        AddRegexDialogViewModel => new AddRegexDialog(),
        EditRegexRuleDialogViewModel => new EditRegexRuleDialog(),
        AddDirectoryBlacklistItemDialogViewModel => new AddDirectoryBlacklistItemDialog(),
        AddFileExtensionBlacklistItemDialogViewModel => new AddFileExtensionBlacklistItemDialog(),
        _ => throw new ArgumentException($"No dialog is registered for {viewModel.GetType().Name}.", nameof(viewModel))
    };
}
