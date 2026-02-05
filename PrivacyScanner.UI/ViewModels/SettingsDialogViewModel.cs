using System.Collections.ObjectModel;
using System.Windows;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class SettingsDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private ObservableCollection<RegexRule> _rules = new();

    public SettingsDialogViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        DeleteRuleCommand = new RelayCommand<RegexRule>(async rule => await OnDeleteRule(rule));
        EditRuleCommand = new RelayCommand<RegexRule>(async rule => await OnEditRule(rule));
        CloseCommand = new RelayCommand(_ => OnClose());
    }

    public ObservableCollection<RegexRule> Rules
    {
        get => _rules;
        set => SetProperty(ref _rules, value);
    }

    public System.Windows.Input.ICommand DeleteRuleCommand { get; }
    public System.Windows.Input.ICommand EditRuleCommand { get; }
    public System.Windows.Input.ICommand CloseCommand { get; }

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        await LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        var result = await _mediator.Send(new GetAllRegexRulesQuery());
        Rules.Clear();
        foreach (var ruleDto in result.Rules)
        {
            Rules.Add(new RegexRule
            {
                RuleId = ruleDto.RuleId,
                RuleName = ruleDto.RuleName,
                Rule = ruleDto.Rule
            });
        }
    }

    private async Task OnDeleteRule(RegexRule? rule)
    {
        if (rule == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Löschen bestätigen",
            $"Möchten Sie die Regel '{rule.RuleName}' wirklich löschen?",
            "SettingsDialogHost");

        if (confirmed)
        {
            try
            {
                await _mediator.Send(new DeleteRegexRuleCommand { RuleId = rule.RuleId });
                Rules.Remove(rule);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Löschen der Regel: {ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private async Task OnEditRule(RegexRule? rule)
    {
        if (rule == null) return;

        var editViewModel = new EditRegexRuleDialogViewModel(_mediator, rule);
        var dialog = new Controls.EditRegexRuleDialog
        {
            DataContext = editViewModel
        };

        editViewModel.RequestClose += () => MaterialDesignThemes.Wpf.DialogHost.Close("SettingsDialogHost");

        await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "SettingsDialogHost");

        // Nach dem Editieren Liste neu laden
        await LoadRulesAsync();
    }

    private void OnClose()
    {
        RequestClose?.Invoke();
    }
}
