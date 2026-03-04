using System;
using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace WhisperSpeechRecognition.Services
{
    public class AudioRecorder : IDisposable
    {
        private WaveInEvent? _waveSource;
        private WaveFileWriter? _waveFile;
        private string? _tempFilePath;

        public string? TempFilePath => _tempFilePath;

        public void StartRecording()
        {
            // すでに録音中の場合はリセット
            StopRecording();

            _tempFilePath = Path.Combine(Path.GetTempPath(), $"WhisperRecording_{Guid.NewGuid()}.wav");

            // マイク入力を設定（16kHz, 16bit, モノラル）
            _waveSource = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            _waveSource.DataAvailable += OnDataAvailable;
            _waveSource.RecordingStopped += OnRecordingStopped;

            _waveFile = new WaveFileWriter(_tempFilePath, _waveSource.WaveFormat);

            _waveSource.StartRecording();
        }

        public string? StopRecording()
        {
            if (_waveSource != null)
            {
                _waveSource.StopRecording();
            }

            return _tempFilePath;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_waveFile != null)
            {
                _waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                _waveFile.Flush();
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            DisposeResources();
        }

        private void DisposeResources()
        {
            if (_waveSource != null)
            {
                _waveSource.DataAvailable -= OnDataAvailable;
                _waveSource.RecordingStopped -= OnRecordingStopped;
                _waveSource.Dispose();
                _waveSource = null;
            }

            if (_waveFile != null)
            {
                _waveFile.Dispose();
                _waveFile = null;
            }
        }

        public void Dispose()
        {
            StopRecording();
            DisposeResources();

            // 一時ファイルが存在する場合は削除する（最終的に使い終わったタイミングで呼ぶ）
            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // 無視
                }
            }
        }
    }
}
