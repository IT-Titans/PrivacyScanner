using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;

/// <summary>The merged set of default and user-saved regex rules.</summary>
public class GetAllRegexRulesQueryResult
{
    public required List<RegexRuleDto> Rules { get; init; }
}
