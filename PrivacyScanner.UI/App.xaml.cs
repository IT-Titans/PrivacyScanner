using System.IO;
using System.Windows;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.UI.ViewModels;
using ITTitans.PrivacyScanner.UI.Services;
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

        Directory.CreateDirectory(path); // wichtig!

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(path, "app-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();


        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<IFileSystem, FileSystemService>();
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
        Log.CloseAndFlush(); // zwingt Serilog, alles sofort zu schreiben
        base.OnExit(e);
    }
}