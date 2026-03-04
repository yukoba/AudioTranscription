using H.NotifyIcon;
using System;
using System.Threading;
using System.Windows;

namespace WhisperSpeechRecognition
{
    public partial class App : Application
    {
        private Mutex? _mutex;
        private TaskbarIcon? _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 複数起動の防止
            const string appName = "WhisperAutoTyperApp";
            _mutex = new Mutex(true, appName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("アプリケーションは既に起動しています。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            // タスクトレイアイコンの初期化とViewModelのバインド
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            if (_trayIcon != null)
            {
                _trayIcon.DataContext = new TrayIconViewModel();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
