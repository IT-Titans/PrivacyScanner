using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllRegexRulesQuery : IRequest<GetAllRegexRulesQueryResult>
{

}
public class GetAllRegexRulesQueryResult
{
    public required List<RegexRuleDto> Rules { get; init; }
}