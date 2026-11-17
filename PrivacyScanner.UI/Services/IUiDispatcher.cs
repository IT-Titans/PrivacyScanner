namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>
/// Runs work on the UI thread. ViewModels receive notifications from the scan on a background thread
/// and must not touch bound state from there; this abstraction is how they hand the work over, and it
/// is what lets a test run that work without a running WPF application.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>Queues <paramref name="action"/> on the UI thread and returns immediately.</summary>
    void Post(Action action);

    /// <summary>Queues <paramref name="action"/> on the UI thread and completes when it has run.</summary>
    Task InvokeAsync(Action action);
}
