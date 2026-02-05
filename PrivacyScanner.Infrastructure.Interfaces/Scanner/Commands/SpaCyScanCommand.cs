using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class SpaCyScanCommand : IRequest<ScanResultDto>
{
    public required FileInfo FilePath { get; init; }

}