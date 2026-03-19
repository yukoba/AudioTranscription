using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AudioTranscription.Views;
using NHotkey;
using NHotkey.Wpf;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AudioTranscription.Services;

public class AppHotkeyManager : IDisposable
{
    private readonly AudioRecorder _audioRecorder;
    private readonly GeminiService? _geminiService;
    private bool _isRecording;
    private OverlayWindow? _overlay;

    public AppHotkeyManager()
    {
        _audioRecorder = new AudioRecorder();

        try
        {
            _geminiService = new GeminiService();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gemini初期化エラー: {ex.Message}\n設定からAPIキーを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        try
        {
            HotkeyManager.Current.AddOrReplace("ToggleRecording", Key.Space, ModifierKeys.Control | ModifierKeys.Shift, OnToggleRecording);
        }
        catch (HotkeyAlreadyRegisteredException)
        {
            MessageBox.Show("ホットキー(Ctrl+Shift+Space)が既に他のアプリケーションで使用されています。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ホットキーの登録に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Dispose()
    {
        try
        {
            HotkeyManager.Current.Remove("ToggleRecording");
        }
        catch
        {
            // ignore
        }

        if (_overlay != null)
        {
            _overlay.Close();
            _overlay = null;
        }

        _audioRecorder.Dispose();
    }

    private void OnToggleRecording(object? sender, HotkeyEventArgs e)
    {
        e.Handled = true; // イベントを消費する

        if (!_isRecording)
            StartRecordingUI();
        else
            StopRecordingUI();
    }

    private void StartRecordingUI()
    {
        _isRecording = true;

        if (_overlay == null) _overlay = new OverlayWindow();

        _overlay.SetStatus("🎤 録音中...");
        _overlay.Show();

        // 録音開始
        _audioRecorder.StartRecording();
    }

    private void StopRecordingUI()
    {
        _isRecording = false;

        if (_overlay != null) _overlay.SetStatus("⏳ 処理中...");

        // 録音停止してファイルパスを取得
        var wavFilePath = _audioRecorder.StopRecording();
        if (wavFilePath != null)
        {
            // 非同期でAPI処理とクリップボード設定を行う
            Task.Run(async () => await ProcessAudioAsync(wavFilePath));
        }
        else
        {
            if (_overlay != null) _overlay.Hide();
        }
    }

    private async Task ProcessAudioAsync(string wavFilePath)
    {
        try
        {
            if (_geminiService == null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Gemini APIが初期化されていません。APIキーを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            // Gemini APIで文字起こしとフィラー除去を一括処理
            Application.Current.Dispatcher.Invoke(() => _overlay?.SetStatus("⏳ 文字起こし中..."));
            var formattedText = await _geminiService.ProcessAudioAsync(wavFilePath);

            if (string.IsNullOrWhiteSpace(formattedText))
            {
                Application.Current.Dispatcher.Invoke(() => _overlay?.SetStatus("⚠️ 音声が認識できませんでした"));
                await Task.Delay(2000);
                return;
            }

            // 直接文字入力を行う (STAスレッド制約のためDispatcherを使用)
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(formattedText))
                {
                    if (formattedText != "@@@")
                    {
                        // SendKeysで送信するために特殊文字をエスケープ
                        var escapedText = Regex.Replace(formattedText, @"[+^%~(){}]", "{$0}")
                            .Replace("\r\n", "{ENTER}")
                            .Replace("\n", "{ENTER}");
                        SendKeys.SendWait(escapedText);
                    }
                    // 完了をシステム音で通知
                    SystemSounds.Asterisk.Play();
                }
            });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "処理エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => { _overlay?.Hide(); });

            // 処理が完了して待機状態に戻る際にメモリを解放
            MemoryHelper.ReleaseMemory();
        }
    }
}
