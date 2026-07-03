using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Adds a new user-defined regex rule.</summary>
public class AddRegexRuleCommand : IRequest<AddRegexRuleCommandResult>
{
    public required string RuleName { get; init; }
    public required string Rule { get; init; }
}
