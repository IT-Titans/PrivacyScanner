using System.IO;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class AddDirectoryBlacklistItemDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public AddDirectoryBlacklistItemDialogViewModel(IMediator mediator)
    {
        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAdd(), _ => CanAdd());
        CancelCommand = new RelayCommand(_ => OnCancel());
    }

    public string DirectoryName
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DirectoryNamePreview));
        }
    } = string.Empty;

    public string DirectoryNamePreview => $"{Path.DirectorySeparatorChar}{TrimDirectoryName(DirectoryName)}{Path.DirectorySeparatorChar}";

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
        return !string.IsNullOrWhiteSpace(DirectoryName);
    }

    private async Task OnAdd()
    {
        ErrorMessage = null;
        try
        {
            _ = await _mediator.Send(new AddDirectoryBlacklistItemCommand()
            {
                DirectoryName = TrimDirectoryName(DirectoryName),
            });

            DirectoryName = string.Empty;

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

    private static string TrimDirectoryName(string directory) => directory.Trim('/').Trim('\\');
}