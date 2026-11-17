using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Backs the dialog for adding a file extension to the scan blacklist.</summary>
public class AddFileExtensionBlacklistItemDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public AddFileExtensionBlacklistItemDialogViewModel(IMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAddAsync(), _ => CanAdd());
        CancelCommand = new RelayCommand(_ => OnCancel());
    }

    public string Extension
    {
        get;
        set
        {
            var normalized = Normalize(value);

            // The text box shows what the user typed. If that differs from the normalized value but the stored
            // value did not change (for example a lone "." in an empty field), the view has to be told to redraw.
            if (!SetProperty(ref field, normalized) && !string.Equals(normalized, value, StringComparison.Ordinal))
            {
                OnPropertyChanged();
            }
        }
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

    /// <summary>
    /// Trims whitespace and leading dots, then prefixes exactly one dot. Input without any characters besides
    /// dots and whitespace becomes the empty string, so a lone "." can neither be shown nor added.
    /// </summary>
    private static string Normalize(string? value)
    {
        var name = value?.Trim().TrimStart('.').Trim() ?? string.Empty;

        return name.Length == 0 ? string.Empty : $".{name}";
    }

    private bool CanAdd()
    {
        return !string.IsNullOrWhiteSpace(Extension);
    }

    private async Task OnAddAsync()
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
