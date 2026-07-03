using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;

/// <summary>
/// Notification published when a scanned file produced one or more warnings.
/// </summary>
public class FoundWarningEvent : INotification
{
    public required ScanResultDto ScanResultDto { get; init; }
}
