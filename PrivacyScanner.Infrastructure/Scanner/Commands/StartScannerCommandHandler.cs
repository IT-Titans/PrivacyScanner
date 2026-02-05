using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class StartScannerCommandHandler(IMediator mediator, IScannerStateService scannerStateService) : IRequestHandler<StartScannerCommand>
{

    public async ValueTask<Unit> Handle(StartScannerCommand request, CancellationToken cancellationToken)
    {
        scannerStateService.Reset();

        await mediator.Send(new ProcessScanCommand
        {
            RootDirectory = request.RootDirectory,
            RegexRuleList = request.RegexRuleList,
            UseSpacy = request.UseSpacy
        }, cancellationToken);

        return Unit.Value;
    }
}