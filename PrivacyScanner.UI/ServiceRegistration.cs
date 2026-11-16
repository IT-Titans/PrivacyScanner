using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.UI;

/// <summary>
/// Registers everything the application resolves from the container, except the main window.
/// </summary>
/// <remarks>
/// This lives outside <c>App.xaml.cs</c> so that a test can build the same container the application
/// builds and resolve from it. A service that is used but not registered then fails a test instead of
/// the first click that needs it. The main window stays with <see cref="App"/>: it can only be created
/// once WPF is running.
/// </remarks>
public static class ServiceRegistration
{
    public static IServiceCollection AddPrivacyScanner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<IDirectoryProvider, DirectoryProvider>();
        services.AddSingleton<IScannerStateService, ScannerStateService>();
        services.AddSingleton<ISettingsPathProvider, SettingsPathProvider>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<MainViewModel>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });

        var loggerFactory = new Serilog.Extensions.Logging.SerilogLoggerFactory();

        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        return services;
    }
}
