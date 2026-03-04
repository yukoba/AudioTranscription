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

        public AppHotkeyManager()
        {
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
        }

        private void StopRecordingUI()
        {
            _isRecording = false;

            if (_overlay != null)
            {
                _overlay.SetStatus("⏳ 処理中...");
                // 実際には処理完了後に閉じるが、今回はStep 2のため少ししたら非表示にするか、そのまま閉じる
                // 今回は即座に隠す
                _overlay.Hide();
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
        }
    }
}