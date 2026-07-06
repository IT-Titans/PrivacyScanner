using System.Diagnostics;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Enumerates the files in the target directory and scans each one, publishing progress and warning events as it goes.
/// </summary>
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

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var fileInfoQueryResult = await mediator.Send(
            new GetAllFilePathsInDirectoryQuery
            {
                RootDirectory = request.RootDirectory,
                DirectoryBlacklistItems = request.DirectoryBlacklistItems,
                FileExtensionBlacklistItems = request.FileExtensionBlacklistItems,
            },
            scanToken);
        var currentFileNumber = 0;

        foreach (var filePath in fileInfoQueryResult.FilePaths)
        {
            if (scanToken.IsCancellationRequested)
            {
                logger.LogInformation("Scan cancelled.");
                break;
            }

            currentFileNumber++;
            await ProcessSingleFileAsync(filePath, request, currentFileNumber, fileInfoQueryResult.FileCount, scanToken);
        }

        stopwatch.Stop();
        logger.LogInformation("Finished scan. Took '{ElapsedSeconds}' second(s)", stopwatch.Elapsed.TotalSeconds);

        return Unit.Value;
    }

    private async Task ProcessSingleFileAsync(
        FileInfo filePath,
        ProcessScanCommand request,
        int currentFileNumber,
        int totalFileCount,
        CancellationToken scanToken)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Processing {FilePath}", filePath);
        }

        bool isBinary;
        try
        {
            isBinary = IsBinaryFile(filePath.FullName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "File: {file} could not be read and is being skipped. Path: {pathName}", filePath.Name, filePath.FullName);
            await PublishFileProcessedEventAsync(currentFileNumber, totalFileCount, scanToken);
            return;
        }

        if (isBinary)
        {
            logger.LogWarning("File: {file} is not readable. Path: {pathName}", filePath.Name, filePath.FullName);
            await PublishFileProcessedEventAsync(currentFileNumber, totalFileCount, scanToken);
            return;
        }

        var scanResultDto = await mediator.Send(new ScanFileCommand
        {
            FilePath = filePath,
            RegexRuleList = request.RegexRuleList,
            UseSpacy = request.UseSpacy
        }, scanToken);

        await PublishFileProcessedEventAsync(currentFileNumber, totalFileCount, scanToken);

        if (scanResultDto.Warnings.Count == 0)
        {
            return;
        }

        await mediator.Publish(new FoundWarningEvent { ScanResultDto = scanResultDto }, scanToken);
    }

    private async Task PublishFileProcessedEventAsync(int currentFileNumber, int totalFileCount, CancellationToken scanToken)
    {
        await mediator.Publish(
            new FileProcessedEvent
            {
                ScannedFilesCount = currentFileNumber,
                TotalFilesCount = totalFileCount,
                ProgressInPercent = GetProgress(currentFileNumber, totalFileCount)
            }, scanToken);
    }

    private int GetProgress(int current, int total)
    {
        if (total == 0)
            return 0;
        return (int)((double)current / total * 100);
    }

    private static bool IsBinaryFile(string filePath)
    {
        const int sampleSize = 8000;

        var buffer = new byte[sampleSize];

        using var stream = File.OpenRead(filePath);
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        for (var i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }
}
