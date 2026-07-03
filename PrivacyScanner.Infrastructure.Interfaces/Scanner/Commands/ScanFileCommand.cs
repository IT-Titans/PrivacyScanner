using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>Scans a single file using regex rules and, if enabled, SpaCy NLP detection.</summary>
public class ScanFileCommand : IRequest<ScanResultDto>
{
    public required FileInfo FilePath { get; init; }
    public required List<RegexRuleDto>? RegexRuleList { get; init; }
    public bool UseSpacy { get; init; }
}
