using System.IO;
using System.Windows;
using System.Windows.Threading;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.UI.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace ITTitans.PrivacyScanner.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner");

        Directory.CreateDirectory(path);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(path, "app-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        RegisterGlobalExceptionHandlers();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Ensures that no unhandled exception can silently crash the app (or vanish without a log entry).
    /// UI-thread exceptions are logged and swallowed so the app keeps running; exceptions on other
    /// threads cannot be safely suppressed but are logged before the process terminates.
    /// </summary>
    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            Log.Logger.Error(e.Exception, "Unhandled exception on the UI thread");
            MessageBox.Show(
                $"Es ist ein unerwarteter Fehler aufgetreten:\n{e.Exception.Message}\n\nDie Anwendung läuft weiter, der Fehler wurde protokolliert.",
                "Unerwarteter Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Logger.Fatal(e.ExceptionObject as Exception, "Unhandled exception outside the UI thread (IsTerminating: {IsTerminating})", e.IsTerminating);
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Logger.Error(e.Exception, "Unobserved exception in a fire-and-forget task");
            e.SetObserved();
        };
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<IDirectoryProvider, DirectoryProvider>();
        services.AddSingleton<IScannerStateService, ScannerStateService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });

        var loggerFactory = new Serilog.Extensions.Logging.SerilogLoggerFactory();

        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider.GetRequiredService<ILogger<App>>().LogInformation("App gestartet");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush(); // forces Serilog to flush everything immediately
        base.OnExit(e);
    }
}
