using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ITTitans.PrivacyScanner.UI.Converters;

[ValueConversion(typeof(string), typeof(Visibility))]
public class EmptyStringToHiddenVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return Visibility.Hidden;
        }

        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Hidden : Visibility.Visible;
        }

        throw new InvalidOperationException($"Cannot convert value of type '{value}'");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}