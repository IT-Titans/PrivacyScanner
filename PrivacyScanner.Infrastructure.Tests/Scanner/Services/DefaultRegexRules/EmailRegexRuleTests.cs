using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services.DefaultRegexRules;

public class EmailRegexRuleTests
{
    private readonly RegexRuleDto _sut = DefaultRegexRulesProvider.GetEmailRegexRule();

    [Fact]
    public void IsEnabledByDefault()
    {
        Assert.True(_sut.IsEnabled);
    }

    [Theory]
    [InlineData("some.valid@mail.com")]
    public void Matches_expected(string input)
    {
        Assert.Matches(_sut.Rule, input);
    }

    [Theory]
    [InlineData("lorem ipsum dolor sit amet")]
    public void Ignores_expected(string input)
    {
        Assert.DoesNotMatch(_sut.Rule, input);
    }
}