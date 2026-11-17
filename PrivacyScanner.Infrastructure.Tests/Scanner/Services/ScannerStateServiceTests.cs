using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using Xunit;

namespace ITTitans.PrivacyScanner.Infrastructure.Tests.Scanner.Services;

/// <summary>Verifies the cancellation state a scan runs under: fresh after creation and after every reset, cancelled after a stop.</summary>
public class ScannerStateServiceTests
{
    [Fact]
    public void Token_AfterCreation_IsNotCancelled()
    {
        using var sut = new ScannerStateService();

        Assert.False(sut.Token.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_CancelsTheCurrentToken()
    {
        using var sut = new ScannerStateService();
        var token = sut.Token;

        sut.Cancel();

        Assert.True(token.IsCancellationRequested);
        Assert.True(sut.Token.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_CalledTwice_DoesNotThrow()
    {
        using var sut = new ScannerStateService();

        sut.Cancel();
        sut.Cancel();

        Assert.True(sut.Token.IsCancellationRequested);
    }

    [Fact]
    public void Reset_AfterCancel_ProvidesFreshTokenAndKeepsOldOneCancelled()
    {
        using var sut = new ScannerStateService();
        var oldToken = sut.Token;
        sut.Cancel();

        sut.Reset();

        Assert.True(oldToken.IsCancellationRequested);
        Assert.False(sut.Token.IsCancellationRequested);
    }

    [Fact]
    public void Reset_WhileRunning_CancelsTheOldScanAndProvidesFreshToken()
    {
        using var sut = new ScannerStateService();
        var oldToken = sut.Token;

        sut.Reset();

        Assert.True(oldToken.IsCancellationRequested);
        Assert.False(sut.Token.IsCancellationRequested);
        Assert.NotEqual(oldToken, sut.Token);
    }

    [Fact]
    public void Reset_CalledTwice_StillProvidesUsableToken()
    {
        using var sut = new ScannerStateService();

        sut.Reset();
        sut.Reset();
        sut.Cancel();

        Assert.True(sut.Token.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_ReleasesTheTokenSource()
    {
        var sut = new ScannerStateService();

        sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sut.Token);
    }
}
