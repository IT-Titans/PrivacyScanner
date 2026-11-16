using System.Threading.Tasks;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>
/// Shows dialogs on behalf of ViewModels. A ViewModel names what it wants shown and hands over the
/// dialog's ViewModel; which control renders it and how it gets on screen is this service's business,
/// which is what keeps the calling code testable without a window.
/// </summary>
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string dialogIdentifier = "RootDialog");

    /// <summary>
    /// Shows the dialog that belongs to <paramref name="viewModel"/> in the dialog host named
    /// <paramref name="dialogIdentifier"/> and completes when the dialog has been closed.
    /// </summary>
    Task ShowDialogAsync(object viewModel, string dialogIdentifier = "RootDialog");

    /// <summary>Closes the dialog currently open in the dialog host named <paramref name="dialogIdentifier"/>.</summary>
    void CloseDialog(string dialogIdentifier = "RootDialog");

    /// <summary>Shows a blocking error message and returns when the user has dismissed it.</summary>
    void ShowError(string message, string title);
}
