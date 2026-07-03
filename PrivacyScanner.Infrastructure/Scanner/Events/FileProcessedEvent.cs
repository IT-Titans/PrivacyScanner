using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;

/// <summary>
/// Notification published after each scanned file to report scan progress.
/// </summary>
public class FileProcessedEvent : INotification
{
    public required int ScannedFilesCount { get; init; }
    public required int TotalFilesCount { get; init; }
    public required int ProgressInPercent { get; init; }
}
