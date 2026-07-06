using System.Windows.Controls;

namespace ITTitans.PrivacyScanner.UI.Controls;

public partial class ConfirmationDialog : UserControl
{
    public ConfirmationDialog(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        InitializeComponent();
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }
}
