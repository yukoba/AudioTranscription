using System;
using System.Windows;
using System.Windows.Input;

namespace WhisperSpeechRecognition
{
    public class TrayIconViewModel
    {
        public ICommand ShowSettingsCommand { get; }
        public ICommand ExitApplicationCommand { get; }
        public TrayIconViewModel()
        {
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }
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
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
