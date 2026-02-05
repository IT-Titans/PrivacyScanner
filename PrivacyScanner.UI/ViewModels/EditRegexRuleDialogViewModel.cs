using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using ITTitans.PrivacyScanner.UI.Models;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class EditRegexRuleDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly Guid _ruleId;
    private string _ruleName;
    private string _regex;
    private string? _errorMessage;

    public EditRegexRuleDialogViewModel(IMediator mediator, RegexRule rule)
    {
        _mediator = mediator;
        _ruleId = rule.RuleId;
        _ruleName = rule.RuleName;
        _regex = rule.Rule;

        UpdateCommand = new RelayCommand(async _ => await OnUpdate(), _ => CanUpdate());
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

    private async Task OnUpdate()
    {
        ErrorMessage = null;
        try
        {
            await _mediator.Send(new EditRegexRuleCommand
            {
                RuleId = _ruleId,
                RuleName = RuleName,
                Rule = Regex
            });
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void OnCancel()
    {
        RequestClose?.Invoke();
    }
}
