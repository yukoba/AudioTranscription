using System.Windows;
using AudioTranscription.Services;
using H.NotifyIcon;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AudioTranscription;

public partial class App : Application
{
    private AppHotkeyManager? _hotkeyManager;
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 複数起動の防止
        const string appName = "AudioTranscriptionAutoTyperApp";
        _mutex = new Mutex(true, appName, out var createdNew);
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
            _trayIcon.ForceCreate();
        }

        // ホットキーマネージャーの初期化
        _hotkeyManager = new AppHotkeyManager();

        // 起動時のWPF内部的な初期化処理が完全に落ち着くのを待ってからメモリを解放する
        Task.Run(async () =>
        {
            await Task.Delay(2000); // 2秒待機
            MemoryHelper.ReleaseMemory();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
