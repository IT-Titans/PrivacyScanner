using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;
using System.Windows.Input;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class AddRegexDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private string _ruleName = string.Empty;
    private string _regex = string.Empty;
    private string? _errorMessage;

    public AddRegexDialogViewModel(IMediator mediator)
    {
        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAdd(), _ => CanAdd());
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

    public System.Windows.Input.ICommand AddCommand { get; }
    public System.Windows.Input.ICommand CancelCommand { get; }

    public event Action? RequestClose;
    public event Action? RequestFocusName;

    private bool CanAdd()
    {
        return !string.IsNullOrWhiteSpace(RuleName) && !string.IsNullOrWhiteSpace(Regex);
    }

    private async Task OnAdd()
    {
        ErrorMessage = null;
        try
        {
            _ = await _mediator.Send(new AddRegexRuleCommand
            {
                RuleName = RuleName,
                Rule = Regex
            });

            RuleName = string.Empty;
            Regex = string.Empty;
            RequestFocusName?.Invoke();
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
