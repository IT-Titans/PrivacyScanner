using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;

public class FoundWarningEvent : INotification
{
    public required ScanResultDto ScanResultDto { get; init; }
}