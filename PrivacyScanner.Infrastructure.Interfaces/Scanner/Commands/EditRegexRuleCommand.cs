using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class EditRegexRuleCommand : IRequest
{
    public string? RuleName { get; init; }
    public string? Rule { get; init; }
    public required Guid RuleId { get; init; }
}