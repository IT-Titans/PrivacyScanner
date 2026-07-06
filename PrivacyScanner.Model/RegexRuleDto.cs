namespace ITTitans.PrivacyScanner.Model;

/// <summary>A named regex pattern used to detect sensitive content during a scan.</summary>
public class RegexRuleDto
{
    // To be extended
    public required string RuleName { get; init; }
    public required string Rule { get; init; }
    public required Guid RuleId { get; init; }
    public bool IsEnabled { get; init; } = true;
}
