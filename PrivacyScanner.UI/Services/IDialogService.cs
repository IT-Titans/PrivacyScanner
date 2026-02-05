using System.Threading.Tasks;

namespace ITTitans.PrivacyScanner.UI.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message, string dialogIdentifier = "RootDialog");
}
