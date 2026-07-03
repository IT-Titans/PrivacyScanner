using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies deletion by RuleId, including the not-found case.</summary>
public class DeleteRegexRuleCommandHandlerTests
{
    private static (DeleteRegexRuleCommandHandler Sut, List<RegexRuleDto> Rules, RegexRuleDto ExistingRule) CreateSutWithOneRule()
    {
        var existingRule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Existing", Rule = @"\d+" };
        var rules = new List<RegexRuleDto> { existingRule };

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllRegexRulesQueryResult>(new GetAllRegexRulesQueryResult { Rules = rules }));

        return (new DeleteRegexRuleCommandHandler(mediatorMock.Object), rules, existingRule);
    }

    [Fact]
    public async Task Handle_ExistingRuleId_RemovesRuleAndReturnsSuccess()
    {
        var (sut, rules, existingRule) = CreateSutWithOneRule();

        var result = await sut.Handle(new DeleteRegexRuleCommand { RuleId = existingRule.RuleId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task Handle_UnknownRuleId_ReturnsFailureAndLeavesListUnchanged()
    {
        var (sut, rules, _) = CreateSutWithOneRule();

        var result = await sut.Handle(new DeleteRegexRuleCommand { RuleId = Guid.NewGuid() }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(rules);
    }
}
