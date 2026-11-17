using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Settings;

/// <summary>
/// The correct behaviour of editing a rule where the current handler is suspected to be wrong. Skipped until the
/// decision on the suspected defect is made; they are written against the behaviour the user expects.
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

    [Fact(Skip = "Suspected defect V3: EditRegexRuleCommandHandler.cs:14-68 does not validate the pattern, only the add handler does. An invalid pattern is saved and later skipped silently by the scan.")]
    public async Task Edit_InvalidPattern_IsRejectedAndNothingIsSaved()
    {
        var (sut, mediator, original) = Create();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = original.RuleId, Rule = "(unbalanced" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        mediator.Verify(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(Skip = "Suspected defect (analysis of 06.11.2026, section 5.7): EditRegexRuleCommandHandler.cs:30-35, 42-47, 54-59 build the edited rule without IsEnabled, so a disabled rule becomes enabled again.")]
    public async Task Edit_KeepsTheEnabledFlagOfADisabledRule()
    {
        var (sut, mediator, original) = Create(isEnabled: false);
        List<RegexRuleDto>? saved = null;
        mediator
            .Setup(m => m.Send(It.IsAny<SaveRegexRulesCommand>(), It.IsAny<CancellationToken>()))
            .Callback((SaveRegexRulesCommand c, CancellationToken _) => saved = c.RegexRuleDtos)
            .Returns(new ValueTask<Unit>(Unit.Value));

        await sut.Handle(new EditRegexRuleCommand { RuleId = original.RuleId, RuleName = "Umbenannt" }, CancellationToken.None);

        Assert.False(Assert.Single(saved!).IsEnabled);
    }
}
