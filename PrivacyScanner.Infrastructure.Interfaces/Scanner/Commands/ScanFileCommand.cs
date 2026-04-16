using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class ScanFileCommand : IRequest<ScanResultDto>
{
    public required FileInfo FilePath { get; init; }
    public required List<RegexRuleDto>? RegexRuleList { get; init; }
    public bool UseSpacy { get; init; }
}
