using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Holds the cancellation token source for the currently running (or most recent) scan.
/// </summary>
public class ScannerStateService : IScannerStateService, IDisposable
{
    private CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public void Cancel()
    {
        _cts.Cancel();
    }

    public void Reset()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        _cts.Dispose();
        _cts = new();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
