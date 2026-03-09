using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AudioTranscription;

public class TrayIconViewModel
{
    public TrayIconViewModel()
    {
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ExitApplicationCommand = new RelayCommand(ExitApplication);
    }

    public ICommand ShowSettingsCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    private void ShowSettings(object? parameter)
    {
        // TODO: 後ほど設定ウィンドウを作成して表示する処理を実装
        MessageBox.Show("設定画面は今後実装されます。", "設定", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitApplication(object? parameter)
    {
        Application.Current.Shutdown();
    }
}

// 簡単な ICommand 実装ヘルパー
public class RelayCommand : ICommand
{
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<object?> _execute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
