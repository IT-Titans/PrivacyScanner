using System.Windows.Input;

namespace ITTitans.PrivacyScanner.UI.Commands;

/// <summary>A typed <see cref="ICommand"/> backed by delegates, requerying via <see cref="CommandManager"/>.</summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || (parameter is T t && _canExecute(t));

    public void Execute(object? parameter) => _execute(parameter is T t ? t : default);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
