using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Commands;

/// <summary>Verifies regex validation and persistence when adding a new user-defined rule.</summary>
public class AddRegexRuleCommandHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(unbalanced")]
    public async Task Handle_InvalidRegex_ReturnsFailureWithoutTouchingStorage(string invalidPattern)
    {
        var sut = new AddRegexRuleCommandHandler(_mediatorMock.Object, Mock.Of<ILogger<AddRegexRuleCommandHandler>>());

        var result = await sut.Handle(
            new AddRegexRuleCommand { RuleName = "Test", Rule = invalidPattern },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRegex_AddsRuleToExistingListAndReturnsSuccess()
    {
        var existingRule = new RegexRuleDto { RuleId = Guid.NewGuid(), RuleName = "Existing", Rule = @"\d+" };
        var rules = new List<RegexRuleDto> { existingRule };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllRegexRulesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetAllRegexRulesQueryResult>(new GetAllRegexRulesQueryResult { Rules = rules }));

        var sut = new AddRegexRuleCommandHandler(_mediatorMock.Object, Mock.Of<ILogger<AddRegexRuleCommandHandler>>());

        var result = await sut.Handle(
            new AddRegexRuleCommand { RuleName = "Neue Regel", Rule = @"[A-Z]+" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, rules.Count);
        Assert.Contains(rules, r => r.RuleName == "Neue Regel" && r.Rule == "[A-Z]+");
        Assert.Contains(rules, r => r.RuleId == existingRule.RuleId);
    }
}
