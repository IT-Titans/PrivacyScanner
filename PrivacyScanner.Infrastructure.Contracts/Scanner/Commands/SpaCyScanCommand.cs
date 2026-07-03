using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Scans a single file for named entities (persons, locations, organizations) via the SpaCy Python bridge.</summary>
public class SpaCyScanCommand : IRequest<ScanResultDto>
{
    public required FileInfo FilePath { get; init; }

}
