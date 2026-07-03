namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>The result of editing a regex rule, with an error message on failure (e.g. rule not found).</summary>
public record EditRegexRuleCommandResult
{
    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public static EditRegexRuleCommandResult Success() => new() { IsSuccess = true };

    public static EditRegexRuleCommandResult Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
