using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;
using OpenAI.Chat;

namespace WhisperSpeechRecognition.Services
{
    public class OpenAIService
    {
        private readonly string _apiKey;
        private readonly AudioClient _audioClient;
        private readonly ChatClient _chatClient;

        public OpenAIService()
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string settingsFilePath = Path.Combine(userProfilePath, "WhisperSpeechRecognition.json");

            // %USERPROFILE%\WhisperSpeechRecognition.json や環境変数から設定を読み込む
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(settingsFilePath, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<OpenAIService>(optional: true)
                .Build();

            _apiKey = config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("OpenAI APIキーが設定されていません。%USERPROFILE%\\WhisperSpeechRecognition.json または環境変数 OPENAI_API_KEY を確認してください。");
            }

            _audioClient = new AudioClient("whisper-1", _apiKey);
            _chatClient = new ChatClient("gpt-5-mini", _apiKey);
        }

        /// <summary>
        /// WAVファイルをWhisper APIに送信して文字起こしを行う
        /// </summary>
        public async Task<string> TranscribeAudioAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"音声ファイルが見つかりません: {filePath}");
            }

            var options = new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Text,
                Language = "ja" // 日本語指定
            };

            AudioTranscription transcription = await _audioClient.TranscribeAudioAsync(filePath, options);
            return transcription.Text;
        }

        /// <summary>
        /// GPTモデルを使って文字起こしテキストのフィラー（えーっと、あのーなど）を除去し整形する
        /// </summary>
        public async Task<string> RemoveFillersAndFormatTextAsync(string transcribedText)
        {
            if (string.IsNullOrWhiteSpace(transcribedText))
            {
                return string.Empty;
            }

            string systemPrompt = "あなたは優秀なテキスト編集アシスタントです。ユーザーの音声認識テキストから『えっと』『あの』『その』などのフィラー（ケバ）や不要な言い淀みを取り除き、自然で読みやすい文章に整形してください。整形後のテキストのみを出力し、解説や挨拶は含めないでください。";

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(transcribedText)
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Content[0].Text;
        }
    }
}
