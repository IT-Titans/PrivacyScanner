using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class AddRegexRuleCommand : IRequest
{
    public required string RuleName { get; init; }
    public required string Rule { get; init; }
}