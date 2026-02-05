using System.Windows.Controls;
using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Controls;

public partial class AddRegexDialog : UserControl
{
    public AddRegexDialog()
    {
        InitializeComponent();
        DataContextChanged += AddRegexDialog_DataContextChanged;
    }

    private void AddRegexDialog_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AddRegexDialogViewModel vm)
        {
            vm.RequestFocusName += () => RuleNameTextBox.Focus();
        }
    }
}
