using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

/// <summary>
/// Merges the built-in default regex rules with the user's saved rules, letting saved rules override defaults by <c>RuleId</c>.
/// </summary>
public class GetAllRegexRulesQueryHandler(ILogger<GetAllRegexRulesQueryHandler> logger) : IRequestHandler<GetAllRegexRulesQuery, GetAllRegexRulesQueryResult>
{
    public async ValueTask<GetAllRegexRulesQueryResult> Handle(GetAllRegexRulesQuery request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner", "rules.json");

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

    private async Task<List<RegexRuleDto>> LoadUserRulesAsync(string path)
    {
        if (!File.Exists(path))
            return new List<RegexRuleDto>();

        try
        {
            var json = await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<List<RegexRuleDto>>(json)
                   ?? new List<RegexRuleDto>();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Konnte gespeicherte Regex-Regeln unter {Path} nicht laden, es werden nur die Standardregeln verwendet", path);
            return new List<RegexRuleDto>();
        }
    }
}
