using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ITTitans.PrivacyScanner.UI.Converters;

/// <summary>Converts a bool (enabled state) to a <see cref="FontWeight"/>, bolding enabled items.</summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ? FontWeights.SemiBold : FontWeights.Normal;
        }

        return FontWeights.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
