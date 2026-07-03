using System.IO;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.UI.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

/// <summary>Backs the dialog for adding a directory name to the scan blacklist.</summary>
public class AddDirectoryBlacklistItemDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public AddDirectoryBlacklistItemDialogViewModel(IMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
        AddCommand = new RelayCommand(async _ => await OnAddAsync(), _ => CanAdd());
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

    private async Task OnAddAsync()
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
