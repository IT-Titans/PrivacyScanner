using ITTitans.PrivacyScanner.UI.Models;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Models;

/// <summary>
/// Covers the rule model shown as chips, in particular that two instances are never equal, whatever their id.
/// </summary>
public class RegexRuleTests
{
    [Fact]
    public void NewRule_IsEnabledWithEmptyTexts()
    {
        var rule = new RegexRule();

        Assert.True(rule.IsEnabled);
        Assert.Equal(Guid.Empty, rule.RuleId);
        Assert.Equal(string.Empty, rule.RuleName);
        Assert.Equal(string.Empty, rule.Rule);
    }

    [Fact]
    public void IsEnabled_NotifiesOnlyOnChange()
    {
        var rule = new RegexRule();
        var names = new List<string?>();
        rule.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        rule.IsEnabled = true;
        rule.IsEnabled = false;

        Assert.Equal(new[] { "IsEnabled" }, names);
        Assert.False(rule.IsEnabled);
    }

    [Fact]
    public void Equality_IsReferenceEquality()
    {
        var id = Guid.NewGuid();
        var a = new RegexRule { RuleId = id, RuleName = "A", Rule = "a" };
        var b = new RegexRule { RuleId = id, RuleName = "A", Rule = "a" };

        Assert.True(a.Equals(a));
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_OtherTypeOrNull_IsFalse()
    {
        var rule = new RegexRule { RuleId = Guid.NewGuid() };

        Assert.False(rule.Equals("text"));
        Assert.False(rule.Equals(null));
    }

    [Fact]
    public void RulesWithSameId_StayApartInAHashSet()
    {
        var id = Guid.NewGuid();
        var set = new HashSet<RegexRule> { new() { RuleId = id }, new() { RuleId = id } };

        Assert.Equal(2, set.Count);
    }
}
