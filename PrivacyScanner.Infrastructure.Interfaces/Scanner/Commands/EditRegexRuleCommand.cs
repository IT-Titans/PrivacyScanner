using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>Partially updates an existing regex rule's name and/or pattern.</summary>
public class EditRegexRuleCommand : IRequest<EditRegexRuleCommandResult>
{
    public string? RuleName { get; init; }
    public string? Rule { get; init; }
    public required Guid RuleId { get; init; }
}
