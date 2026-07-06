using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Persists the full set of regex rules, overwriting the previously saved state.</summary>
public class SaveRegexRulesCommand : IRequest
{
    public required List<RegexRuleDto> RegexRuleDtos { get; init; }
}
