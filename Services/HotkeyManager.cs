using System;
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

        public AppHotkeyManager()
        {
            _audioRecorder = new AudioRecorder();

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
                _overlay.Hide();
            }

            // 録音停止してファイルパスを取得
            string? wavFilePath = _audioRecorder.StopRecording();
            if (wavFilePath != null)
            {
                // ToDo: Step 4でOpenAI APIにこのファイルを渡す
                Console.WriteLine($"録音完了: {wavFilePath}");
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
