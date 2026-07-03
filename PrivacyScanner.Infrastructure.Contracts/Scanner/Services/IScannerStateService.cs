namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

/// <summary>Tracks cancellation state for the currently running scan.</summary>
public interface IScannerStateService
{
    CancellationToken Token { get; }
    void Cancel();
    void Reset();
}
