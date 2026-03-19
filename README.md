# Windows音声文字起こしアプリ
Google の Gemini 3 Flash を使用して音声認識を行い、「えっと」などの言い淀みを除去して、アクティブなウィンドウに認識結果を入力します。Gemini API 料金は1時間で約18円です。2026年3月19日現在、Google 本家の Android の音声認識よりも認識精度が良いです。

## 使用方法
インストールすると、スタートメニューに緑のマイクアイコンの Audio Transcription が追加になるので、起動すると、タスクトレイに常駐します。`Ctrl + Shift + スペース` を押すと、録音が始まり、もう一度 `Ctrl + Shift + スペース` を押すと認識が終了します。認識結果はアクティブなウィンドウに入力されます。漢字変換はオフにしておいてください。

タスクトレイのアイコンを右クリックすると終了できます。

## インストール方法
https://github.com/yukoba/AudioTranscription/releases からインストールしてください。

Gemini を使用しているので API キーは https://aistudio.google.com/app/apikey から作成してください。
その API キーを環境変数 `GEMINI_API_KEY` に書くか、もしくは、以下の内容で `%USERPROFILE%\AudioTranscription.json` に書いてください。

```json
{
	"Gemini": {
		"ApiKey": "AAAAAAAAA"
	}
}
```

## ライセンス
アイコンは https://www.flaticon.com/free-icon/circle_14025057 より。ソースコードはMITライセンスです。
