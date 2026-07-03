using System.Windows.Input;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Backs the dialog for adding a new user-defined regex rule.</summary>
public class AddRegexDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private string _ruleName = string.Empty;
    private string _regex = string.Empty;
    private string? _errorMessage;

    public AddRegexDialogViewModel(IMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAddAsync(), _ => CanAdd());
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

    private async Task OnAddAsync()
    {
        ErrorMessage = null;

        var result = await _mediator.Send(new AddRegexRuleCommand
        {
            RuleName = RuleName,
            Rule = Regex
        });

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        RuleName = string.Empty;
        Regex = string.Empty;
        RequestFocusName?.Invoke();
        RequestClose?.Invoke();
    }

    private void OnCancel()
    {
        RequestClose?.Invoke();
    }
}
