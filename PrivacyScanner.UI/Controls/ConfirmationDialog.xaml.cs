using System.Windows.Controls;

namespace ITTitans.PrivacyScanner.UI.Controls;

public partial class ConfirmationDialog : UserControl
{
    public ConfirmationDialog(string title, string message)
    {
        InitializeComponent();
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }
}
