using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services.DefaultRegexRules;

/// <summary>
/// Verifies the shipped default rule set as a whole (stable, unique, enabled-by-default RuleIds — see
/// GetAllRegexRulesQueryHandler's merge-by-RuleId contract) plus match/non-match behavior of each
/// individual rule not already covered by <see cref="EmailRegexRuleTests"/> or <see cref="IbanRegexRuleTests"/>.
/// </summary>
public class DefaultRegexRulesProviderTests
{
    [Fact]
    public void GetDefaultRules_ReturnsEightRulesWithUniqueIds_AllEnabledByDefault()
    {
        var rules = DefaultRegexRulesProvider.GetDefaultRules();

        Assert.Equal(8, rules.Count);
        Assert.Equal(rules.Count, rules.Select(r => r.RuleId).Distinct().Count());
        Assert.All(rules, r => Assert.True(r.IsEnabled));
    }

    [Theory]
    [InlineData("+49 30 12345678")]
    [InlineData("030 12345678")]
    public void Telefonnummer_Matches_expected(string input)
    {
        AssertRuleMatches("Telefonnummer (DE)", input);
    }

    [Theory]
    [InlineData("lorem ipsum dolor sit amet")]
    public void Telefonnummer_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Telefonnummer (DE)", input);
    }

    [Theory]
    [InlineData("12345")]
    public void Postleitzahl_Matches_expected(string input)
    {
        AssertRuleMatches("Postleitzahl (DE)", input);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    public void Postleitzahl_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Postleitzahl (DE)", input);
    }

    [Theory]
    [InlineData("AB1234567")]
    public void Personalausweisnummer_Matches_expected(string input)
    {
        AssertRuleMatches("Personalausweisnummer (DE)", input);
    }

    [Theory]
    [InlineData("AB123456")]
    public void Personalausweisnummer_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Personalausweisnummer (DE)", input);
    }

    [Theory]
    [InlineData("4111111111111111")]
    public void Kreditkartennummer_Matches_expected(string input)
    {
        AssertRuleMatches("Kreditkartennummer", input);
    }

    [Theory]
    [InlineData("1234567890123456")]
    public void Kreditkartennummer_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Kreditkartennummer", input);
    }

    [Theory]
    [InlineData("31.12.2023")]
    [InlineData("2023-12-31")]
    public void Datum_Matches_expected(string input)
    {
        AssertRuleMatches("Datum (dd.mm.yyyy / yyyy-mm-dd)", input);
    }

    [Theory]
    [InlineData("lorem ipsum dolor sit amet")]
    public void Datum_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Datum (dd.mm.yyyy / yyyy-mm-dd)", input);
    }

    [Theory]
    [InlineData("Musterstraße 12")]
    public void Adresse_Matches_expected(string input)
    {
        AssertRuleMatches("Adresse (Straße + Hausnummer)", input);
    }

    [Theory]
    [InlineData("12345")]
    public void Adresse_Ignores_expected(string input)
    {
        AssertRuleDoesNotMatch("Adresse (Straße + Hausnummer)", input);
    }

    private static void AssertRuleMatches(string ruleName, string input)
    {
        var rule = FindRule(ruleName);
        Assert.Matches(rule.Rule, input);
    }

    private static void AssertRuleDoesNotMatch(string ruleName, string input)
    {
        var rule = FindRule(ruleName);
        Assert.DoesNotMatch(rule.Rule, input);
    }

    private static Model.RegexRuleDto FindRule(string ruleName) =>
        DefaultRegexRulesProvider.GetDefaultRules().Single(r => r.RuleName == ruleName);
}
