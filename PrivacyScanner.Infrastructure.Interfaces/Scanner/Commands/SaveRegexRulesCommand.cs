using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class SaveRegexRulesCommand : IRequest
{
    public required List<RegexRuleDto> RegexRuleDtos { get; init; }
}