using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Events;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ICommand = System.Windows.Input.ICommand;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class MainViewModel : ViewModelBase, INotificationHandler<FoundWarningEvent>, INotificationHandler<FileScannedEvent>, INotificationHandler<FileProcessedEvent>
{
    private const int MaxLogEntries = 1000;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ICsvExportService _csvExportService;
    private string _selectedPath = string.Empty;
    private string _exportFilePath = string.Empty;
    private string _regexSearchText = string.Empty;
    private double _progress;
    private int _scannedFilesCount;
    private int _totalFilesCount;
    private ObservableCollection<RegexRule> _allRegexRules = new();
    private ObservableCollection<RegexRule> _filteredRegexRules = new();
    private BulkObservableCollection<LogEntryViewModel> _logEntries = new();
    private bool _isScanning;
    private string _statusText = "Bereit";
    private int counter = 0;
    private int pro_counter = 0;
    private int _totalWarningsCount;
    private int _regexWarningsCount;
    private int _spacyWarningsCount;
    private bool _isSpacyEnabled;
    private bool _isSpacyAvailable;
    private bool _isProgressIndeterminate;
    private bool _wasScanCancelled;

    public MainViewModel(IMediator mediator, ILogger<MainViewModel> logger, IDialogService dialogService, ICsvExportService csvExportService)
    {
        _mediator = mediator;
        _logger = logger;
        _dialogService = dialogService;
        _csvExportService = csvExportService;

        SelectPathCommand = new RelayCommand(_ => OnSelectPath(), _ => !IsScanning);
        SelectExportFileCommand = new RelayCommand(_ => OnSelectExportFile(), _ => !IsScanning);
        ClearExportFileCommand = new RelayCommand(_ => OnClearExportFile(), _ => !IsScanning && !string.IsNullOrEmpty(ExportFilePath));
        ToggleScanCommand = new RelayCommand(_ => OnToggleScan(), _ => CanStartScan());
        StartScanCommand = new RelayCommand(_ => OnStartScan(), _ => CanStartScan() && !IsScanning);
        StopScanCommand = new RelayCommand(_ => OnStopScan(), _ => IsScanning);
        ToggleRuleCommand = new RelayCommand<RegexRule>(rule => OnToggleRule(rule), _ => !IsScanning);
        OpenSettingsCommand = new RelayCommand(_ => OnOpenSettings(), _ => !IsScanning);
        AddNewRegexCommand = new RelayCommand(_ => OnAddNewRegex(), _ => !IsScanning);
        OpenFileCommand = new RelayCommand<string>(path => OnOpenFile(path));

        UpdateFilteredRegexRules();
    }

    public ICommand OpenFileCommand { get; }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    (ToggleScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StartScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SelectPathCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ToggleRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (OpenSettingsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (AddNewRegexCommand as RelayCommand)?.RaiseCanExecuteChanged();
                });
                OnPropertyChanged(nameof(ScanButtonText));
                OnPropertyChanged(nameof(ScanButtonIcon));
                OnPropertyChanged(nameof(IsConfigurationEnabled));
                OnPropertyChanged(nameof(IsSpacyCheckboxEnabled));
            }
        }
    }

    public bool IsConfigurationEnabled => !IsScanning;

    public bool IsSpacyCheckboxEnabled => IsConfigurationEnabled && _isSpacyAvailable;

    public string SpacyCheckboxContent => _isSpacyAvailable
        ? "SpaCy aktivieren"
        : "SpaCy aktivieren (nicht verfügbar)";

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set
        {
            if (SetProperty(ref _isProgressIndeterminate, value))
            {
                OnPropertyChanged(nameof(ProgressDisplayText));
            }
        }
    }

    public string ProgressDisplayText => IsProgressIndeterminate ? "Dateien werden gezählt..." : $"{Progress:0}%";

    public bool IsSpacyEnabled
    {
        get => _isSpacyEnabled;
        set
        {
            if (_isSpacyEnabled != value)
            {
                if (value)
                {
                    // Show confirmation dialog when enabling SpaCy
                    _ = ConfirmSpacyActivationAsync();
                }
                else
                {
                    SetProperty(ref _isSpacyEnabled, value);
                    RaiseCanExecuteChanged();
                }
            }
        }
    }

    private async Task ConfirmSpacyActivationAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "SpaCy aktivieren",
            "Möchten Sie SpaCy wirklich aktivieren? SpaCy kann potentiell viele False-Positives erzeugen.");

        if (confirmed)
        {
            SetProperty(ref _isSpacyEnabled, true, nameof(IsSpacyEnabled));
            RaiseCanExecuteChanged();
        }
        else
        {
            // Notify UI that the checkbox should remain unchecked
            OnPropertyChanged(nameof(IsSpacyEnabled));
        }
    }

    private bool CanStartScan()
    {
        // Pfad muss ausgewählt sein
        if (string.IsNullOrWhiteSpace(SelectedPath))
            return false;

        // Wenn SpaCy aktiviert ist, kann der Scan gestartet werden
        if (IsSpacyEnabled)
            return true;

        // Wenn SpaCy nicht aktiviert ist, muss mindestens eine Regex-Regel aktiviert sein
        return _allRegexRules.Any(r => r.IsEnabled);
    }

    private void RaiseCanExecuteChanged()
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            (ToggleScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
        });
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ScanButtonText => IsScanning ? "Scan stoppen" : "Scan starten";
    public string ScanButtonIcon => IsScanning ? "Stop" : "Play";

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (SetProperty(ref _selectedPath, value))
            {
                (StartScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ExportFilePath
    {
        get => _exportFilePath;
        set
        {
            if (SetProperty(ref _exportFilePath, value))
            {
                (ClearExportFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string RegexSearchText
    {
        get => _regexSearchText;
        set
        {
            if (SetProperty(ref _regexSearchText, value))
            {
                UpdateFilteredRegexRules();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
            {
                OnPropertyChanged(nameof(ProgressDisplayText));
            }
        }
    }

    public int ScannedFilesCount
    {
        get => _scannedFilesCount;
        set => SetProperty(ref _scannedFilesCount, value);
    }

    public int TotalFilesCount
    {
        get => _totalFilesCount;
        set => SetProperty(ref _totalFilesCount, value);
    }

    public int TotalWarningsCount
    {
        get => _totalWarningsCount;
        set => SetProperty(ref _totalWarningsCount, value);
    }

    public int RegexWarningsCount
    {
        get => _regexWarningsCount;
        set => SetProperty(ref _regexWarningsCount, value);
    }

    public int SpacyWarningsCount
    {
        get => _spacyWarningsCount;
        set => SetProperty(ref _spacyWarningsCount, value);
    }

    public ObservableCollection<RegexRule> FilteredRegexRules => _filteredRegexRules;
    public BulkObservableCollection<LogEntryViewModel> LogEntries => _logEntries;

    public ICommand SelectPathCommand { get; }
    public ICommand SelectExportFileCommand { get; }
    public ICommand ClearExportFileCommand { get; }
    public ICommand ToggleScanCommand { get; }
    public ICommand StartScanCommand { get; }
    public ICommand StopScanCommand { get; }

    public ICommand ToggleRuleCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand AddNewRegexCommand { get; }

    private void OnOpenFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("Datei konnte nicht geöffnet werden: {FilePath}", filePath);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Öffnen der Datei: {FilePath}", filePath);
        }
    }

    private void OnSelectPath()
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SelectedPath = dialog.FolderName;
        }
    }

    private async void OnSelectExportFile()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
            DefaultExt = ".csv",
            Title = "Export-Datei auswählen"
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = dialog.FileName;

            if (File.Exists(selectedPath))
            {
                var overwrite = await _dialogService.ShowConfirmationAsync(
                    "Datei existiert bereits",
                    $"Die Datei \"{Path.GetFileName(selectedPath)}\" existiert bereits. Beim nächsten Scan wird sie überschrieben. Möchten Sie diese Datei trotzdem verwenden?");

                if (!overwrite)
                {
                    return;
                }
            }

            ExportFilePath = selectedPath;
        }
    }

    private void OnClearExportFile()
    {
        ExportFilePath = string.Empty;
    }

    private async void OnToggleScan()
    {
        if (IsScanning)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Scan stoppen",
                "Möchten Sie den laufenden Scan wirklich abbrechen?");

            if (confirmed)
            {
                _wasScanCancelled = true;
                OnStopScan();
            }
        }
        else
        {
            OnStartScan();
        }
    }

    private void OnStartScan()
    {
        LogEntries.Clear();
        Progress = 0;
        ScannedFilesCount = 0;
        TotalFilesCount = 0;
        StatusText = "Scan läuft...";
        var rootDirectory = new DirectoryInfo(SelectedPath);

        List<RegexRuleDto> regexRuleDtoList = new List<RegexRuleDto>();
        foreach (RegexRule regexrule in _allRegexRules.Where(r => r.IsEnabled).ToList())
        {
            RegexRuleDto regexruledto = new RegexRuleDto()
            {
                Rule = regexrule.Rule,
                RuleId = regexrule.RuleId,
                RuleName = regexrule.RuleName,
                IsEnabled = regexrule.IsEnabled
            };

            regexRuleDtoList.Add(regexruledto);
        }

        IsScanning = true;
        Progress = 0;
        ScannedFilesCount = 0;
        TotalFilesCount = 0;
        LogEntries.Clear();
        counter = 0;
        pro_counter = 0;
        TotalWarningsCount = 0;
        RegexWarningsCount = 0;
        SpacyWarningsCount = 0;
        _wasScanCancelled = false;
        IsProgressIndeterminate = true;

        if (!string.IsNullOrEmpty(ExportFilePath))
        {
            try
            {
                _csvExportService.InitializeExport(ExportFilePath);
                _logger.LogInformation("CSV-Export gestartet: {FilePath}", ExportFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Initialisieren des CSV-Exports: {FilePath}", ExportFilePath);
            }
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Send(new StartScannerCommand
                {
                    RootDirectory = rootDirectory,
                    RegexRuleList = regexRuleDtoList,
                    UseSpacy = IsSpacyEnabled
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scan was cancelled by user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scan");
            }
            finally
            {
                IsScanning = false;
                ResetUI();
            }
        });
    }

    private void ResetUI()
    {
        _csvExportService.Close();
        _logger.LogInformation("CSV-Export abgeschlossen");

        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsProgressIndeterminate = false;
            if (_wasScanCancelled)
            {
                StatusText = "Scan abgebrochen";
                // Progress bleibt beim aktuellen Wert
            }
            else
            {
                Progress = 100;
                StatusText = "Scan abgeschlossen";
            }
            counter = 0;
            pro_counter = 0;
            OnPropertyChanged(nameof(ScanButtonText));
            OnPropertyChanged(nameof(ScanButtonIcon));
        });
    }

    private void OnStopScan()
    {
        _ = _mediator.Send(new StopScannerCommand());
    }

    private async void OnToggleRule(RegexRule? rule)
    {
        if (rule != null)
        {
            rule.IsEnabled = !rule.IsEnabled;
            RaiseCanExecuteChanged();
            await SaveRuleStatesAsync();
        }
    }

    private async Task SaveRuleStatesAsync()
    {
        var ruleDtos = _allRegexRules.Select(r => new RegexRuleDto
        {
            RuleId = r.RuleId,
            RuleName = r.RuleName,
            Rule = r.Rule,
            IsEnabled = r.IsEnabled
        }).ToList();

        await _mediator.Send(new SaveRegexRulesCommand { RegexRuleDtos = ruleDtos });
    }

    private async void OnOpenSettings()
    {
        var dialogViewModel = new SettingsDialogViewModel(_mediator, _dialogService);
        var dialog = new Controls.SettingsDialog
        {
            DataContext = dialogViewModel
        };

        dialogViewModel.RequestClose += () => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

        _ = dialogViewModel.InitializeAsync();

        await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");

        // Nach dem Schließen des Dialogs laden wir die Regeln neu, 
        // um sicherzustellen, dass die Haupt-UI aktuell ist.
        await LoadRegexRulesAsync();
    }

    private async void OnAddNewRegex()
    {
        var dialogViewModel = new AddRegexDialogViewModel(_mediator);
        var dialog = new Controls.AddRegexDialog
        {
            DataContext = dialogViewModel
        };

        dialogViewModel.RequestClose += () => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

        var result = await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");

        // Nach dem Schließen des Dialogs laden wir die Regeln neu, 
        // um sicherzustellen, dass alles aktuell ist.
        await LoadRegexRulesAsync();
    }

    private void UpdateFilteredRegexRules()
    {
        _filteredRegexRules.Clear();
        var search = RegexSearchText.ToLower();
        foreach (var rule in _allRegexRules)
        {
            if (string.IsNullOrWhiteSpace(search) || rule.RuleName.ToLower().Contains(search))
            {
                _filteredRegexRules.Add(rule);
            }
        }
    }

    public ValueTask Handle(FoundWarningEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"File #{++counter}");

        // Elemente außerhalb des UI-Threads vorbereiten
        var items = notification.ScanResultDto.Warnings.Select(warning => new LogEntryViewModel
        {
            FilePath = notification.ScanResultDto.FilePath.FullName,
            Line = warning.Line,
            SuspiciousContent = warning.SuspiciousContent,
            Position = warning.Start,
            End = warning.End,
            Type = warning.Type,
            SpacyLabel = warning.SpacyLabel,
            RuleName = warning.RuleName
        }).ToList();

        // Zähle die Warnungen nach Typ
        int regexCount = items.Count(i => i.Type == ScanWarningType.Rule);
        int spacyCount = items.Count(i => i.Type == ScanWarningType.SpaCy);

        // CSV-Export (synchronisiert mit den Scan-Ergebnissen)
        if (_csvExportService.IsExportActive)
        {
            foreach (var item in items)
            {
                try
                {
                    _csvExportService.WriteEntry(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Schreiben in CSV-Export");
                }
            }
        }

        // Asynchron und im Batch an den Dispatcher übergeben, um UI-Last zu minimieren
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            LogEntries.AddRange(items, MaxLogEntries);
            TotalWarningsCount += items.Count;
            RegexWarningsCount += regexCount;
            SpacyWarningsCount += spacyCount;
        }));

        return ValueTask.CompletedTask;
    }

    public ValueTask Handle(FileScannedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"new prgress{notification.ProgressInPercent}");
        _logger.LogInformation($"File #{++pro_counter}");
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            Progress = notification.ProgressInPercent;
        }));

        return ValueTask.CompletedTask;
    }

    public ValueTask Handle(FileProcessedEvent notification, CancellationToken cancellationToken)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            // Indeterminate-Modus beenden, sobald wir die Gesamtzahl kennen
            if (IsProgressIndeterminate && notification.TotalFilesCount > 0)
            {
                IsProgressIndeterminate = false;
            }
            ScannedFilesCount = notification.ScannedFilesCount;
            TotalFilesCount = notification.TotalFilesCount;
            Progress = notification.ProgressInPercent;
        }));

        return ValueTask.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        var envCheck = await _mediator.Send(new CheckPythonAndSpacyEnvironmentQuery());
        _isSpacyAvailable = envCheck.IsValid;
        OnPropertyChanged(nameof(IsSpacyCheckboxEnabled));
        OnPropertyChanged(nameof(SpacyCheckboxContent));

        if (!envCheck.IsValid)
        {
            _logger.LogWarning("SpaCy ist nicht verfügbar: {Message}", envCheck.UserMessage);
        }

        await LoadRegexRulesAsync();
    }


    private async Task LoadRegexRulesAsync()
    {
        var regexRulesFromQuery = (await _mediator.Send(new GetAllRegexRulesQuery())).Rules;
        _allRegexRules.Clear();
        foreach (var rule in regexRulesFromQuery)
        {
            _allRegexRules.Add(new RegexRule
            {
                RuleId = rule.RuleId,
                Rule = rule.Rule,
                RuleName = rule.RuleName,
                IsEnabled = rule.IsEnabled
            });
        }
        UpdateFilteredRegexRules();
        RaiseCanExecuteChanged();
    }
}
