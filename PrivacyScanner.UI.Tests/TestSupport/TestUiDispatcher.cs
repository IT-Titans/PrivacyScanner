using ITTitans.PrivacyScanner.UI.Services;

namespace ITTitans.PrivacyScanner.UI.Tests.TestSupport;

/// <summary>
/// Stands in for the WPF dispatcher. By default it runs the work on the calling thread at once, so a
/// test sees the result on the next line. With <see cref="RunImmediately"/> switched off it collects
/// the work instead, which is how a test proves that something was handed to the dispatcher and not
/// done directly.
/// </summary>
internal sealed class TestUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> _pending = new();

    public bool RunImmediately { get; set; } = true;

    public int PendingCount => _pending.Count;

    public void Post(Action action)
    {
        if (RunImmediately)
        {
            action();
        }
        else
        {
            _pending.Enqueue(action);
        }
    }

    public Task InvokeAsync(Action action)
    {
        Post(action);

        return Task.CompletedTask;
    }

    public void RunPending()
    {
        while (_pending.TryDequeue(out var action))
        {
            action();
        }
    }
}
