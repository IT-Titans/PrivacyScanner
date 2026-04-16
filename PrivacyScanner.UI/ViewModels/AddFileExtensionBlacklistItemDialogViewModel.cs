using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class AddFileExtensionBlacklistItemDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public AddFileExtensionBlacklistItemDialogViewModel(IMediator mediator)
    {
        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAdd(), _ => CanAdd());
        CancelCommand = new RelayCommand(_ => OnCancel());
    }

    public string Extension
    {
        get;
        set => SetProperty(ref field, value.StartsWith('.') ? value : $".{value}");
    } = string.Empty;

    public string? ErrorMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public System.Windows.Input.ICommand AddCommand { get; }

    public System.Windows.Input.ICommand CancelCommand { get; }

    public event Action? RequestClose;

    public event Action? RequestFocusName;

    private bool CanAdd()
    {
        return !string.IsNullOrWhiteSpace(Extension);
    }

    private async Task OnAdd()
    {
        ErrorMessage = null;

        try
        {
            _ = await _mediator.Send(new AddFileExtensionBlacklistItemCommand()
            {
                Extension = Extension,
            });

            Extension = string.Empty;

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