using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using System.Windows;
using WhisperSpeechRecognition.Views;

namespace WhisperSpeechRecognition.Services
{
    public class AppHotkeyManager : IDisposable
    {
        private OverlayWindow? _overlay;
        private bool _isRecording;
        private readonly AudioRecorder _audioRecorder;
        private OpenAIService? _openAIService;

        public AppHotkeyManager()
        {
            _audioRecorder = new AudioRecorder();

            try
            {
                _openAIService = new OpenAIService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OpenAI初期化エラー: {ex.Message}\n設定からAPIキーを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                NHotkey.Wpf.HotkeyManager.Current.AddOrReplace("ToggleRecording", Key.Space, ModifierKeys.Control | ModifierKeys.Shift, OnToggleRecording);
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

        private void OnToggleRecording(object? sender, HotkeyEventArgs e)
        {
            e.Handled = true; // イベントを消費する

            if (!_isRecording)
            {
                StartRecordingUI();
            }
            else
            {
                StopRecordingUI();
            }
        }

        private void StartRecordingUI()
        {
            _isRecording = true;

            if (_overlay == null)
            {
                _overlay = new OverlayWindow();
            }
            
            _overlay.SetStatus("🎤 録音中...");
            _overlay.Show();

            // 録音開始
            _audioRecorder.StartRecording();
        }

        private void StopRecordingUI()
        {
            _isRecording = false;

            if (_overlay != null)
            {
                _overlay.SetStatus("⏳ 処理中...");
            }

            // 録音停止してファイルパスを取得
            string? wavFilePath = _audioRecorder.StopRecording();
            if (wavFilePath != null)
            {
                // 非同期でAPI処理とクリップボード設定を行う
                Task.Run(async () => await ProcessAudioAsync(wavFilePath));
            }
            else
            {
                if (_overlay != null)
                {
                    _overlay.Hide();
                }
            }
        }

        private async Task ProcessAudioAsync(string wavFilePath)
        {
            try
            {
                if (_openAIService == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("OpenAI APIが初期化されていません。APIキーを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                // 1. Whisper APIで文字起こし
                Application.Current.Dispatcher.Invoke(() => _overlay?.SetStatus("⏳ 文字起こし中..."));
                string transcribedText = await _openAIService.TranscribeAudioAsync(wavFilePath);

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    Application.Current.Dispatcher.Invoke(() => _overlay?.SetStatus("⚠️ 音声が認識できませんでした"));
                    await Task.Delay(2000);
                    return;
                }

                // 2. GPTモデルでフィラー除去・整形
                Application.Current.Dispatcher.Invoke(() => _overlay?.SetStatus("⏳ テキスト整形中..."));
                string formattedText = await _openAIService.RemoveFillersAndFormatTextAsync(transcribedText);

                // 3. クリップボードへの設定 (STAスレッド制約のためDispatcherを使用)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(formattedText))
                    {
                        Clipboard.SetText(formattedText);
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlay?.Hide();
                });
            }
        }

        public void Dispose()
        {
            try
            {
                NHotkey.Wpf.HotkeyManager.Current.Remove("ToggleRecording");
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
    }
}
