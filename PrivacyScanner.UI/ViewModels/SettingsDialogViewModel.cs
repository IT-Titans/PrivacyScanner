using System.Collections.ObjectModel;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using ITTitans.PrivacyScanner.UI.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Backs the settings dialog for listing, editing, and deleting regex rules.</summary>
public class SettingsDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private ObservableCollection<RegexRule> _rules = new();

    public SettingsDialogViewModel(IMediator mediator, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(dialogService);

        _mediator = mediator;
        _dialogService = dialogService;
        DeleteRuleCommand = new RelayCommand<RegexRule>(async rule => await OnDeleteRuleAsync(rule));
        EditRuleCommand = new RelayCommand<RegexRule>(async rule => await OnEditRuleAsync(rule));
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

    private async Task OnDeleteRuleAsync(RegexRule? rule)
    {
        if (rule == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Löschen bestätigen",
            $"Möchten Sie die Regel '{rule.RuleName}' wirklich löschen?",
            "SettingsDialogHost");

        if (confirmed)
        {
            var result = await _mediator.Send(new DeleteRegexRuleCommand { RuleId = rule.RuleId });

            if (result.IsSuccess)
            {
                Rules.Remove(rule);
            }
            else
            {
                _dialogService.ShowError(
                    $"Fehler beim Löschen der Regel: {result.ErrorMessage}",
                    "Fehler");
            }
        }
    }

    private async Task OnEditRuleAsync(RegexRule? rule)
    {
        if (rule == null)
            return;

        var editViewModel = new EditRegexRuleDialogViewModel(_mediator, rule);

        editViewModel.RequestClose += () => _dialogService.CloseDialog("SettingsDialogHost");

        await _dialogService.ShowDialogAsync(editViewModel, "SettingsDialogHost");

        // Reload the list after editing
        await LoadRulesAsync();
    }

    private void OnClose()
    {
        RequestClose?.Invoke();
    }
}
