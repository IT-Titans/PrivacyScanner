using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class DeleteRegexRuleCommand : IRequest
{
    public required Guid RuleId { get; init; }
}