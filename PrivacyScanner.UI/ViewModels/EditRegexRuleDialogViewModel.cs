using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Backs the dialog for editing an existing regex rule's name and pattern.</summary>
public class EditRegexRuleDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly Guid _ruleId;
    private string _ruleName;
    private string _regex;
    private string? _errorMessage;

    public EditRegexRuleDialogViewModel(IMediator mediator, RegexRule rule)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(rule);

        _mediator = mediator;
        _ruleId = rule.RuleId;
        _ruleName = rule.RuleName;
        _regex = rule.Rule;

        UpdateCommand = new RelayCommand(async _ => await OnUpdateAsync(), _ => CanUpdate());
        CancelCommand = new RelayCommand(_ => OnCancel());
    }

    public string RuleName
    {
        get => _ruleName;
        set => SetProperty(ref _ruleName, value);
    }

    public string Regex
    {
        get => _regex;
        set => SetProperty(ref _regex, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public System.Windows.Input.ICommand UpdateCommand { get; }
    public System.Windows.Input.ICommand CancelCommand { get; }

    public event Action? RequestClose;

    private bool CanUpdate()
    {
        return !string.IsNullOrWhiteSpace(RuleName) && !string.IsNullOrWhiteSpace(Regex);
    }

    private async Task OnUpdateAsync()
    {
        ErrorMessage = null;

        var result = await _mediator.Send(new EditRegexRuleCommand
        {
            RuleId = _ruleId,
            RuleName = RuleName,
            Rule = Regex
        });

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        RequestClose?.Invoke();
    }

    private void OnCancel()
    {
        RequestClose?.Invoke();
    }
}
