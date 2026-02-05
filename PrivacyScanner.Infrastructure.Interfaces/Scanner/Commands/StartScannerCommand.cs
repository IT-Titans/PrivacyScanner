using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class StartScannerCommand : IRequest
{
    public required DirectoryInfo RootDirectory { get; init; }

    public required List<RegexRuleDto>? RegexRuleList { get; init; }

    public bool UseSpacy { get; init; }
}