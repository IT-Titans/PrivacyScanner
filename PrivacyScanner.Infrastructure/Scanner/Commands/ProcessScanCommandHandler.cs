using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class ProcessScanCommandHandler(
    IMediator mediator,
    IScannerStateService scannerStateService,
    ILogger<ProcessScanCommandHandler> logger
) : IRequestHandler<ProcessScanCommand>
{
    public async ValueTask<Unit> Handle(
        ProcessScanCommand request,
        CancellationToken cancellationToken)
    {
        var scanToken = scannerStateService.Token;

        var fileInfoQueryResult = await mediator.Send(
            new GetAllFilePathsInDirectoryQuery
            {
                RootDirectory = request.RootDirectory
            },
            scanToken);
        var currentFileNumber = 0;
        var lastPublishedProgress = -1;


        foreach (var filePath in fileInfoQueryResult.FilePaths)
        {
            if (scanToken.IsCancellationRequested)
            {
                logger.LogInformation("Scan cancelled.");
                break;
            }

            logger.LogInformation("Processing {FilePath}", filePath);

            currentFileNumber++;

            int currentProgress;
            if (IsBinaryFile(filePath.FullName))
            {
                logger.LogWarning("File: {file} is not readable. Path: {pathName}", filePath.Name, filePath.FullName);

                currentProgress = GetProgress(currentFileNumber, fileInfoQueryResult.FileCount);

                await mediator.Publish(
                    new FileProcessedEvent
                    {
                        ScannedFilesCount = currentFileNumber,
                        TotalFilesCount = fileInfoQueryResult.FileCount,
                        ProgressInPercent = currentProgress
                    }, scanToken);

                if (currentProgress > lastPublishedProgress || currentFileNumber == fileInfoQueryResult.FileCount)
                {
                    await mediator.Publish(
                        new FileScannedEvent
                        {
                            ProgressInPercent = currentProgress,
                        }, scanToken);
                    lastPublishedProgress = currentProgress;
                }

                continue;
            }

            var scanResultDto = await mediator.Send(new ScanFileCommand
            {
                FilePath = filePath,
                RegexRuleList = request.RegexRuleList,
                UseSpacy = request.UseSpacy
            }, scanToken);

            var warnings = scanResultDto.Warnings;

            currentProgress = GetProgress(currentFileNumber, fileInfoQueryResult.FileCount);

            await mediator.Publish(
                new FileProcessedEvent
                {
                    ScannedFilesCount = currentFileNumber,
                    TotalFilesCount = fileInfoQueryResult.FileCount,
                    ProgressInPercent = currentProgress
                }, scanToken);

            if (currentProgress > lastPublishedProgress || currentFileNumber == fileInfoQueryResult.FileCount)
            {
                await mediator.Publish(
                    new FileScannedEvent
                    {
                        ProgressInPercent = currentProgress,
                    }, scanToken);
                lastPublishedProgress = currentProgress;
            }


            if (warnings.Count == 0)
                continue;

            await mediator.Publish(
                new FoundWarningEvent
                {
                    ScanResultDto = scanResultDto
                },
                scanToken);
        }

        return Unit.Value;
    }

    private int GetProgress(int current, int total)
    {
        if (total == 0) return 0;
        return (int)((double)current / total * 100);
    }

    public static bool IsBinaryFile(string filePath)
    {
        const int sampleSize = 8000;

        byte[] buffer = new byte[sampleSize];

        using var stream = File.OpenRead(filePath);
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == 0)
            {

                return true;
            }
        }

        return false;
    }
}
