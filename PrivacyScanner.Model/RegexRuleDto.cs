namespace ITTitans.PrivacyScanner.Model;

public class RegexRuleDto
{
    //Wird noch erweitert
    public required string RuleName { get; init; }
    public required string Rule { get; init; }
    public required Guid RuleId { get; init; }
    public bool IsEnabled { get; init; } = true;
}