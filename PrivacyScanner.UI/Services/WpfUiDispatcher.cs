namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>
/// <see cref="IUiDispatcher"/> backed by the dispatcher of the running WPF application.
/// </summary>
/// <remarks>
/// The application is looked up on every call instead of once in the constructor. That is what the
/// ViewModels did before this type existed, and it keeps the instance constructible before the
/// application object is.
/// </remarks>
public class WpfUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
    }

    public async Task InvokeAsync(Action action)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(action);
    }
}
