using System.Threading.Tasks;
using ITTitans.PrivacyScanner.UI.Controls;
using MaterialDesignThemes.Wpf;

namespace ITTitans.PrivacyScanner.UI.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message, string dialogIdentifier = "RootDialog")
    {
        var dialog = new ConfirmationDialog(title, message);
        var result = await DialogHost.Show(dialog, dialogIdentifier);

        return result is bool boolResult && boolResult;
    }
}
