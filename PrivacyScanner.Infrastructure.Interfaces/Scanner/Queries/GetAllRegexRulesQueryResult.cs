using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllRegexRulesQueryResult
{
    public required List<RegexRuleDto> Rules { get; init; }
}