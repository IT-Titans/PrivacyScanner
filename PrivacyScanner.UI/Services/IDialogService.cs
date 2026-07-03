using System.Threading.Tasks;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Shows user confirmation dialogs, abstracted away from ViewModels for testability.</summary>
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string dialogIdentifier = "RootDialog");
}
