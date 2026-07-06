using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies the partial-update branching (name only / pattern only / both / neither) when editing a rule.</summary>
public class EditRegexRuleCommandHandlerTests
{
    private static (EditRegexRuleCommandHandler Sut, List<RegexRuleDto> Rules, RegexRuleDto OriginalRule) CreateSutWithOneRule()
    {
        var originalRule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Original", Rule = @"\d+" };
        var rules = new List<RegexRuleDto> { originalRule };

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllRegexRulesQueryResult>(new GetAllRegexRulesQueryResult { Rules = rules }));

        return (new EditRegexRuleCommandHandler(mediatorMock.Object), rules, originalRule);
    }

    [Fact]
    public async Task Handle_RuleIdNotFound_ReturnsFailureAndLeavesListUnchanged()
    {
        var (sut, rules, _) = CreateSutWithOneRule();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = Guid.NewGuid(), RuleName = "Whatever" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(rules);
    }

    [Fact]
    public async Task Handle_NeitherRuleNorRuleNameProvided_ReturnsFailure()
    {
        var (sut, rules, originalRule) = CreateSutWithOneRule();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = originalRule.RuleId },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(rules);
        Assert.Equal("Original", rules[0].RuleName);
    }

    [Fact]
    public async Task Handle_OnlyRuleNameProvided_KeepsOriginalPattern()
    {
        var (sut, rules, originalRule) = CreateSutWithOneRule();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = originalRule.RuleId, RuleName = "Renamed" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(rules);
        Assert.Equal("Renamed", updated.RuleName);
        Assert.Equal(originalRule.Rule, updated.Rule);
        Assert.Equal(originalRule.RuleId, updated.RuleId);
    }

    [Fact]
    public async Task Handle_OnlyRuleProvided_KeepsOriginalName()
    {
        var (sut, rules, originalRule) = CreateSutWithOneRule();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = originalRule.RuleId, Rule = @"[a-z]+" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(rules);
        Assert.Equal(originalRule.RuleName, updated.RuleName);
        Assert.Equal("[a-z]+", updated.Rule);
    }

    [Fact]
    public async Task Handle_RuleAndRuleNameProvided_UpdatesBoth()
    {
        var (sut, rules, originalRule) = CreateSutWithOneRule();

        var result = await sut.Handle(
            new EditRegexRuleCommand { RuleId = originalRule.RuleId, RuleName = "Renamed", Rule = @"[a-z]+" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(rules);
        Assert.Equal("Renamed", updated.RuleName);
        Assert.Equal("[a-z]+", updated.Rule);
        Assert.Equal(originalRule.RuleId, updated.RuleId);
    }
}
