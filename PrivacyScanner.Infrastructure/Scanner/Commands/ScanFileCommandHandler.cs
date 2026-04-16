using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class ScanFileCommandHandler(IMediator mediator, ILogger<ScanFileCommandHandler> logger) : IRequestHandler<ScanFileCommand, ScanResultDto>
{
    public async ValueTask<ScanResultDto> Handle(ScanFileCommand request, CancellationToken cancellationToken)
    {
        var warnings = new List<ScanWarningDto>();

        if (request.UseSpacy)
        {
            try
            {
                var spaCyScanResult = await mediator.Send(
                    new SpaCyScanCommand { FilePath = request.FilePath },
                    cancellationToken);

                warnings.AddRange(spaCyScanResult.Warnings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim SpaCy-Scan für {FilePath}", request.FilePath.FullName);
            }
        }

        if (request.RegexRuleList is { Count: > 0 })
        {
            try
            {
                var regexScanResult = await mediator.Send(
                    new RegexScanCommand { FilePath = request.FilePath, RegexRuleList = request.RegexRuleList },
                    cancellationToken);

                warnings.AddRange(regexScanResult.Warnings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Regex-Scan für {FilePath}", request.FilePath.FullName);
            }
        }

        return new ScanResultDto
        {
            FilePath = request.FilePath,
            Warnings = warnings
        };
    }
}
