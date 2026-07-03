namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>The result of deleting a regex rule, with an error message on failure (e.g. rule not found).</summary>
public record DeleteRegexRuleCommandResult
{
    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public static DeleteRegexRuleCommandResult Success() => new() { IsSuccess = true };

    public static DeleteRegexRuleCommandResult Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
