using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ITTitans.PrivacyScanner.UI.Converters;

public class BoolToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush EnabledBrush = new(Color.FromRgb(7, 122, 186)); // SecondaryColor #077aba
    private static readonly SolidColorBrush DisabledBrush = new(Color.FromRgb(80, 80, 80)); // Gray

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
        {
            return isEnabled ? EnabledBrush : DisabledBrush;
        }
        return DisabledBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
