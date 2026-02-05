using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public record CheckPythonAndSpacyEnvironmentQuery : IRequest<CheckPythonAndSpacyEnvironmentQueryResult>;

public record CheckPythonAndSpacyEnvironmentQueryResult
{
    public bool IsValid { get; init; }

    public string? UserMessage { get; init; }

    public string? TechnicalMessage { get; init; }

    public static CheckPythonAndSpacyEnvironmentQueryResult Success() => new() { IsValid = true };

    public static CheckPythonAndSpacyEnvironmentQueryResult Failure(string userMessage, string? technicalMessage = null)
        => new() { IsValid = false, UserMessage = userMessage, TechnicalMessage = technicalMessage };
}