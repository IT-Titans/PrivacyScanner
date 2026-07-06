using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Deletes a regex rule by its ID.</summary>
public class DeleteRegexRuleCommand : IRequest<DeleteRegexRuleCommandResult>
{
    public required Guid RuleId { get; init; }
}
