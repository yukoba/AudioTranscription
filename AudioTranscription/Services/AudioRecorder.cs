using System.IO;
using NAudio.Wave;

namespace AudioTranscription.Services;

public class AudioRecorder : IDisposable
{
    private WaveFileWriter? _waveFile;
    private WaveInEvent? _waveSource;

    public string? TempFilePath { get; private set; }

    public void Dispose()
    {
        StopRecording();
        DisposeResources();

        // 一時ファイルが存在する場合は削除する（最終的に使い終わったタイミングで呼ぶ）
        if (TempFilePath != null && File.Exists(TempFilePath))
            try
            {
                File.Delete(TempFilePath);
            }
            catch
            {
                // 無視
            }
    }

    public void StartRecording()
    {
        // すでに録音中の場合はリセット
        StopRecording();

        TempFilePath = Path.Combine(Path.GetTempPath(), $"AudioTranscriptionRecording_{Guid.NewGuid()}.wav");

        // マイク入力を設定（16kHz, 16bit, モノラル）
        _waveSource = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1)
        };

        _waveSource.DataAvailable += OnDataAvailable;
        _waveSource.RecordingStopped += OnRecordingStopped;

        _waveFile = new WaveFileWriter(TempFilePath, _waveSource.WaveFormat);

        _waveSource.StartRecording();
    }

    public string? StopRecording()
    {
        if (_waveSource != null) _waveSource.StopRecording();

        return TempFilePath;
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
}
