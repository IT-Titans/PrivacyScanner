using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ITTitans.PrivacyScanner.UI.Converters;

/// <summary>Converts a bool to <see cref="Visibility"/>, inverted: true collapses, false shows.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
