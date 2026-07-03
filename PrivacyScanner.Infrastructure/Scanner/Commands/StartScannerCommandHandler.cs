using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Resets the scanner's cancellation state and kicks off a new scan.
/// </summary>
public class StartScannerCommandHandler(IMediator mediator, IScannerStateService scannerStateService) : IRequestHandler<StartScannerCommand>
{

    public async ValueTask<Unit> Handle(StartScannerCommand request, CancellationToken cancellationToken)
    {
        scannerStateService.Reset();

        await mediator.Send(new ProcessScanCommand
        {
            RootDirectory = request.RootDirectory,
            RegexRuleList = request.RegexRuleList,
            DirectoryBlacklistItems = request.DirectoryBlacklistItems,
            FileExtensionBlacklistItems = request.FileExtensionBlacklistItems,
            UseSpacy = request.UseSpacy
        }, cancellationToken);

        return Unit.Value;
    }
}
