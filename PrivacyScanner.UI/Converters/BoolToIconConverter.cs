using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace ITTitans.PrivacyScanner.UI.Converters;

public class BoolToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            // Show minus when enabled (to disable), plus when disabled (to enable)
            return isEnabled ? PackIconKind.MinusCircle : PackIconKind.PlusCircle;
        }
        return PackIconKind.PlusCircle;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
