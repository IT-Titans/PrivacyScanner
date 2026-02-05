using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;

public class FileScannedEvent : INotification
{
    public required int ProgressInPercent { get; init; }
}