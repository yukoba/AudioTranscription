using System.IO;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Environment = System.Environment;
using File = System.IO.File;

namespace AudioTranscription.Services;

public class GeminiService
{
    private readonly string _apiKey;
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

        _apiKey = config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                "Gemini APIキーが設定されていません。%USERPROFILE%\\AudioTranscription.json または環境変数 GEMINI_API_KEY を確認してください。");

        _client = new Client(apiKey: _apiKey);
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

        var prompt = "音声の内容を文字起こししてください。その際、「えっと」や「あの」などのフィラーは除去し、句読点を付けてください。" +
            "整形後のテキストのみを出力し、解説や挨拶は含めないでください。音声が無音だった場合は `@@@` と返してください。";

        var content = new Content
        {
            Parts = new List<Part>
            {
                Part.FromText(prompt),
                new() { InlineData = new Blob { Data = audioBytes, MimeType = mimeType } }
            },
            Role = "user"
        };

        var config = new GenerateContentConfig
        {
            ThinkingConfig = new ThinkingConfig { ThinkingLevel = ThinkingLevel.Minimal }
        };

        var response = await _client.Models.GenerateContentAsync(
            "gemini-3.1-flash-lite-preview",
            content,
            config
        );

        return response.Text ?? string.Empty;
    }
}
