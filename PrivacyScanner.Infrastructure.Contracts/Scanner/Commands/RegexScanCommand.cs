using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Scans a single file against a set of regex rules.</summary>
public class RegexScanCommand : IRequest<ScanResultDto>
{
    public required FileInfo FilePath { get; init; }

    public required List<RegexRuleDto> RegexRuleList { get; init; }
}
