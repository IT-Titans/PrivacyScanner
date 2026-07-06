using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Cancels the currently running scan.
/// </summary>
public class StopScannerCommandHandler(IScannerStateService scannerStateService) : IRequestHandler<StopScannerCommand>
{
    public ValueTask<Unit> Handle(StopScannerCommand request, CancellationToken cancellationToken)
    {
        scannerStateService.Cancel();

        return new ValueTask<Unit>(Unit.Value);
    }
}
