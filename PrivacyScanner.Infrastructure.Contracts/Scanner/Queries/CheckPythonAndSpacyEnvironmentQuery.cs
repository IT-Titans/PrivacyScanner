using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;

/// <summary>Checks whether Python 3.12, spaCy, and the German language model are correctly installed.</summary>
public record CheckPythonAndSpacyEnvironmentQuery : IRequest<CheckPythonAndSpacyEnvironmentQueryResult>;
