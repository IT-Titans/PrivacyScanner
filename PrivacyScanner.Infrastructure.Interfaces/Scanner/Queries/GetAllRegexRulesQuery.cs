using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

/// <summary>Retrieves the merged set of default and user-saved regex rules.</summary>
public class GetAllRegexRulesQuery : IRequest<GetAllRegexRulesQueryResult>;
