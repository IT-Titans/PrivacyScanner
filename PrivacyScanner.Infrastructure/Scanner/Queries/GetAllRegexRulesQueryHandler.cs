using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

public class GetAllRegexRulesQueryHandler : IRequestHandler<GetAllRegexRulesQuery, GetAllRegexRulesQueryResult>
{
    public async ValueTask<GetAllRegexRulesQueryResult> Handle(GetAllRegexRulesQuery request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner");
        path = Path.Combine(path, "rules.json");

        var userRules = await LoadUserRulesAsync(path);
        var defaultRules = DefaultRegexRulesProvider.GetDefaultRules();

        // Merge: default rules first, then user rules (user rules can override defaults by RuleId)
        var userRuleIds = userRules.Select(r => r.RuleId).ToHashSet();
        var mergedRules = defaultRules
            .Where(d => !userRuleIds.Contains(d.RuleId))
            .Concat(userRules)
            .ToList();

        return await new ValueTask<GetAllRegexRulesQueryResult>(new GetAllRegexRulesQueryResult
        {
            Rules = mergedRules
        });
    }

    private static async Task<List<RegexRuleDto>> LoadUserRulesAsync(string path)
    {
        if (!File.Exists(path))
            return new List<RegexRuleDto>();

        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<List<RegexRuleDto>>(json)
               ?? new List<RegexRuleDto>();
    }
}