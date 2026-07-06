namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>The result of adding a regex rule, with an error message on failure (e.g. an invalid pattern).</summary>
public record AddRegexRuleCommandResult
{
    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public static AddRegexRuleCommandResult Success() => new() { IsSuccess = true };

    public static AddRegexRuleCommandResult Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
