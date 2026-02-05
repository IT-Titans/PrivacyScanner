using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;

public class FileProcessedEvent : INotification
{
    public required int ScannedFilesCount { get; init; }
    public required int TotalFilesCount { get; init; }
    public required int ProgressInPercent { get; init; }
}
