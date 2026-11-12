using System.Globalization;
using System.Windows;
using System.Windows.Media;

using ITTitans.PrivacyScanner.UI.Converters;

using MaterialDesignThemes.Wpf;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Converters;

/// <summary>
/// Covers the value converters that sit between the view models and the XAML.
/// </summary>
/// <remarks>
/// A converter is bound by a string in a XAML file, so a wrong branch here shows up as
/// a control that silently looks wrong rather than as an exception. The fallback branch
/// matters as much as the happy path: it is what runs whenever a binding delivers null
/// or an unexpected type, which is exactly when nobody is watching.
/// </remarks>
public class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public class BoolToBackground
    {
        private readonly BoolToBackgroundConverter _converter = new();

        [Fact]
        public void UsesTheBrandColourWhenEnabled()
        {
            var brush = Assert.IsType<SolidColorBrush>(_converter.Convert(true, typeof(Brush), null, Culture));

            Assert.Equal(Color.FromRgb(7, 122, 186), brush.Color);
        }

        [Fact]
        public void UsesGreyWhenDisabled()
        {
            var brush = Assert.IsType<SolidColorBrush>(_converter.Convert(false, typeof(Brush), null, Culture));

            Assert.Equal(Color.FromRgb(80, 80, 80), brush.Color);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("true")]
        [InlineData(1)]
        public void FallsBackToGreyForAnythingThatIsNotABool(object? value)
        {
            // A binding that delivers the wrong type must not light a rule up as enabled.
            var brush = Assert.IsType<SolidColorBrush>(_converter.Convert(value, typeof(Brush), null, Culture));

            Assert.Equal(Color.FromRgb(80, 80, 80), brush.Color);
        }

        [Fact]
        public void DoesNotSupportConvertingBack()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(null, typeof(bool), null, Culture));
        }
    }

    public class BoolToFontWeight
    {
        private readonly BoolToFontWeightConverter _converter = new();

        [Fact]
        public void BoldsAnEnabledItem()
        {
            Assert.Equal(FontWeights.SemiBold, _converter.Convert(true, typeof(FontWeight), null, Culture));
        }

        [Fact]
        public void LeavesADisabledItemAtNormalWeight()
        {
            Assert.Equal(FontWeights.Normal, _converter.Convert(false, typeof(FontWeight), null, Culture));
        }

        [Fact]
        public void FallsBackToNormalWeight()
        {
            Assert.Equal(FontWeights.Normal, _converter.Convert(null, typeof(FontWeight), null, Culture));
        }

        [Fact]
        public void DoesNotSupportConvertingBack()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(null, typeof(bool), null, Culture));
        }
    }

    public class BoolToIcon
    {
        private readonly BoolToIconConverter _converter = new();

        [Fact]
        public void OffersToDisableWhatIsCurrentlyEnabled()
        {
            // The icon is the action, not the state: an enabled rule shows a minus.
            Assert.Equal(PackIconKind.MinusCircle, _converter.Convert(true, typeof(PackIconKind), null, Culture));
        }

        [Fact]
        public void OffersToEnableWhatIsCurrentlyDisabled()
        {
            Assert.Equal(PackIconKind.PlusCircle, _converter.Convert(false, typeof(PackIconKind), null, Culture));
        }

        [Fact]
        public void FallsBackToOfferingToEnable()
        {
            Assert.Equal(PackIconKind.PlusCircle, _converter.Convert(null, typeof(PackIconKind), null, Culture));
        }

        [Fact]
        public void DoesNotSupportConvertingBack()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(null, typeof(bool), null, Culture));
        }
    }

    public class EmptyStringToHiddenVisibility
    {
        private readonly EmptyStringToHiddenVisibilityConverter _converter = new();

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void HidesBlankText(string value)
        {
            Assert.Equal(Visibility.Hidden, _converter.Convert(value, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void HidesNull()
        {
            Assert.Equal(Visibility.Hidden, _converter.Convert(null, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void ShowsTextThatHasContent()
        {
            Assert.Equal(Visibility.Visible, _converter.Convert("IBAN", typeof(Visibility), null, Culture));
        }

        [Fact]
        public void RefusesAValueThatIsNotAString()
        {
            // This converter throws rather than guessing, which is the one place in the
            // set where a wrong binding fails loudly. Worth pinning down.
            Assert.Throws<InvalidOperationException>(
                () => _converter.Convert(42, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void DoesNotSupportConvertingBack()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(null, typeof(string), null, Culture));
        }
    }

    public class InverseBoolToVisibility
    {
        private readonly InverseBoolToVisibilityConverter _converter = new();

        [Fact]
        public void CollapsesOnTrue()
        {
            Assert.Equal(Visibility.Collapsed, _converter.Convert(true, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void ShowsOnFalse()
        {
            Assert.Equal(Visibility.Visible, _converter.Convert(false, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void FallsBackToVisible()
        {
            // Defaulting to Collapsed here would hide controls whenever a binding breaks,
            // which is far harder to notice than a stray visible one.
            Assert.Equal(Visibility.Visible, _converter.Convert(null, typeof(Visibility), null, Culture));
        }

        [Fact]
        public void DoesNotSupportConvertingBack()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(null, typeof(bool), null, Culture));
        }
    }
}
