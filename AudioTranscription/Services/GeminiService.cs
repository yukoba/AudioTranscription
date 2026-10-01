using System.IO;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Environment = System.Environment;
using File = System.IO.File;

namespace AudioTranscription.Services;

public class GeminiService
{
    private readonly Client _client;

    public GeminiService()
    {
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var settingsFilePath = Path.Combine(userProfilePath, "AudioTranscription.json");

        // %USERPROFILE%\AudioTranscription.json や環境変数から設定を読み込む
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(settingsFilePath, true, true)
            .AddEnvironmentVariables()
            .AddUserSecrets<GeminiService>(true)
            .Build();

        var apiKey = config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Gemini APIキーが設定されていません。%USERPROFILE%\\AudioTranscription.json または環境変数 GEMINI_API_KEY を確認してください。");

        _client = new Client(apiKey: apiKey);
    }

    /// <summary>
    ///     音声ファイルをGemini APIに送信して文字起こしとフィラー除去を一括で行う
    /// </summary>
    public async Task<string> ProcessAudioAsync(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"音声ファイルが見つかりません: {filePath}");

        var audioBytes = await File.ReadAllBytesAsync(filePath);
        File.Delete(filePath);
        var mimeType = filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? "audio/mp3" : "audio/wav";

        var content = new Content
        {
            Parts = [new Part { InlineData = new Blob { Data = audioBytes, MimeType = mimeType } }],
            Role = "user",
        };

        var config = new GenerateContentConfig
        {
            AudioTranscriptionConfig = new AudioTranscriptionConfig
            {
                Mode = "SMART",
                LanguageCodes = ["ja-JP"], // 日本語の音声を想定
            },
        };

        var response = await _client.Models.GenerateContentAsync(
            "gemini-3.5-transcribe",
            content,
            config
        );

        return response.Text ?? string.Empty;
    }
}
