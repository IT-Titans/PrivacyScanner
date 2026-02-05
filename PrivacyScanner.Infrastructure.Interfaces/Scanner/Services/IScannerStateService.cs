namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

public interface IScannerStateService
{
    CancellationToken Token { get; }
    void Cancel();
    void Reset();
}
