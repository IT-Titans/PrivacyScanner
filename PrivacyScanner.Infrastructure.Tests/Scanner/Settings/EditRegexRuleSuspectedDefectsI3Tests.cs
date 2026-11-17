using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>
/// Editing a rule: the pattern is validated like on adding, and the enabled state survives the edit.
/// </summary>
public class EditRegexRuleSuspectedDefectsI3Tests
{
    private static (EditRegexRuleCommandHandler Sut, Mock<IMediator> Mediator, RegexRuleDto Original) Create(bool isEnabled = true)
    {
        var original = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Original", Rule = @"\d+", IsEnabled = isEnabled };
        var rules = new List<RegexRuleDto> { original };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllRegexRulesQueryResult>(new GetAllRegexRulesQueryResult { Rules = rules }));
        mediator
            .Setup(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Unit>(Unit.Value));
        return (new EditRegexRuleCommandHandler(mediator.Object), mediator, original);
    }

    private static List<RegexRuleDto> Saved(Mock<IMediator> mediator) =>
        mediator.Invocations.Select(i => i.Arguments[0]).OfType<SaveRegexRulesCommand>().Single().RegexRuleDtos;

    [Fact]
    public async Task Edit_InvalidPattern_IsRejectedAndNothingIsSaved()
    {
        var (sut, mediator, original) = Create();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = original.RuleId, Rule = "(unbalanced" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        mediator.Verify(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Edit_KeepsTheEnabledFlagOfADisabledRule()
    {
        var (sut, mediator, original) = Create(isEnabled: false);

        await sut.Handle(new EditRegexRuleCommand { RuleId = original.RuleId, RuleName = "Umbenannt" }, CancellationToken.None);

        Assert.False(Assert.Single(Saved(mediator)).IsEnabled);
    }

    [Theory]
    [InlineData("(unbalanced")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Edit_InvalidOrBlankPattern_ReturnsGermanErrorMessage(string pattern)
    {
        var (sut, mediator, original) = Create();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = original.RuleId, RuleName = "Neu", Rule = pattern },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Der eingegebene Regex ist ungültig", result.ErrorMessage);
        mediator.Verify(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Edit_ValidPattern_IsSaved()
    {
        var (sut, mediator, original) = Create();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = original.RuleId, Rule = @"[a-z]+" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(@"[a-z]+", Assert.Single(Saved(mediator)).Rule);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Edit_KeepsEnabledFlagAndOtherFields_InEveryBranch(bool changeName, bool changePattern)
    {
        var (sut, mediator, original) = Create(isEnabled: false);

        await sut.Handle(
            new EditRegexRuleCommand
            {
                RuleId = original.RuleId,
                RuleName = changeName ? "Umbenannt" : null,
                Rule = changePattern ? @"\w+" : null
            },
            CancellationToken.None);

        var edited = Assert.Single(Saved(mediator));
        Assert.False(edited.IsEnabled);
        Assert.Equal(original.RuleId, edited.RuleId);
        Assert.Equal(changeName ? "Umbenannt" : "Original", edited.RuleName);
        Assert.Equal(changePattern ? @"\w+" : @"\d+", edited.Rule);
    }
}
