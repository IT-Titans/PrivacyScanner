using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services.DefaultRegexRules;

/// <summary>
/// Verifies the built-in IBAN regex rule in detail.
/// <para>
/// The pattern is <c>\b[A-Z]{2}[0-9]{2}(?:[ ]?[0-9]{4}){3,6}(?:[ ]?[0-9]{1,4})?\b</c>: two uppercase
/// letters, two check digits, then 3 to 6 groups of 4 digits (each optionally preceded by a single
/// space) and an optional trailing group of 1 to 4 digits. That means it only recognizes IBANs whose
/// BBAN is purely numeric and between 12 and 28 digits long (14 to 30 digits including the check
/// digits) — this covers most European countries' account structure (Germany, Austria, Switzerland,
/// Spain, Belgium, Poland, ...) but deliberately not countries whose BBAN embeds letters (Netherlands,
/// United Kingdom, ...) or the small number of countries below the minimum length (Norway). An earlier
/// version of this pattern capped out at 18 digits total and could not even match a real German IBAN
/// (22 characters, 20 digits) — the boundary tests below exist to keep that regression from recurring.
/// </para>
/// </summary>
public class IbanRegexRuleTests
{
    private readonly RegexRuleDto _sut = DefaultRegexRulesProvider.GetDefaultRules().Single(r => r.RuleName == "IBAN");

    [Fact]
    public void IsEnabledByDefault()
    {
        Assert.True(_sut.IsEnabled);
    }

    [Theory]
    [InlineData("DE89370400440532013000")] // Germany, unspaced, 22 chars / 18-digit BBAN
    [InlineData("DE89 3704 0044 0532 0130 00")] // Germany, spaced in groups of 4
    [InlineData("AT611904300234573201")] // Austria, 20 chars / 16-digit BBAN
    [InlineData("CH9300762011623852957")] // Switzerland, 21 chars / 17-digit BBAN
    [InlineData("ES9121000418450200051332")] // Spain, 24 chars / 20-digit BBAN
    [InlineData("BE68539007547034")] // Belgium, 16 chars / 12-digit BBAN — this pattern's shortest supported BBAN
    [InlineData("PL61109010140000071219812874")] // Poland, 28 chars / 24-digit BBAN — near the upper end of what this pattern supports
    public void Matches_RealWorldNumericIbans(string input)
    {
        Assert.Matches(_sut.Rule, input);
    }

    [Fact]
    public void Matches_WhenEmbeddedInSurroundingSentence()
    {
        const string text = "Bitte überweisen Sie den Betrag auf folgende IBAN: DE89 3704 0044 0532 0130 00 - vielen Dank.";

        Assert.Matches(_sut.Rule, text);
    }

    [Theory]
    [InlineData("lorem ipsum dolor sit amet")] // no IBAN-shaped content at all
    [InlineData("NL91ABNA0417164300")] // Netherlands — BBAN starts with a 4-letter bank code, not purely numeric
    [InlineData("GB29NWBK60161331926819")] // United Kingdom — BBAN starts with a 4-letter bank code, not purely numeric
    [InlineData("NO9386011117947")] // Norway — shortest real-world IBAN (13 digits after the country code), below this pattern's 14-digit minimum
    [InlineData("de89370400440532013000")] // lowercase country code — real IBANs are always written uppercase, so this is intentionally not matched
    [InlineData("DE89  3704 0044 0532 0130 00")] // double space — the pattern only tolerates zero or one space between groups
    public void DoesNotMatch_KnownNonMatches(string input)
    {
        Assert.DoesNotMatch(_sut.Rule, input);
    }

    [Fact]
    public void Matches_AtMinimumSupportedLength_14DigitsAfterCountryCode()
    {
        // 2 check digits + 3 mandatory groups of 4 = 14 digits, the pattern's documented lower bound
        Assert.Matches(_sut.Rule, "XX00111122223333");
    }

    [Fact]
    public void DoesNotMatch_OneDigitBelowMinimumSupportedLength()
    {
        Assert.DoesNotMatch(_sut.Rule, "XX0011112222333");
    }

    [Fact]
    public void Matches_AtMaximumSupportedLength_30DigitsAfterCountryCode()
    {
        // 2 check digits + 6 groups of 4 + a trailing group of 4 = 30 digits, the pattern's documented upper bound
        Assert.Matches(_sut.Rule, "XX001111222233334444555566667777");
    }

    [Fact]
    public void DoesNotMatch_OneDigitAboveMaximumSupportedLength()
    {
        Assert.DoesNotMatch(_sut.Rule, "XX0011112222333344445555666677778");
    }
}
