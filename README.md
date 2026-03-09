# Windows音声文字起こしアプリ
Google の Gemini 3 Flash を使用して音声認識を行い、同時に「えっと」などの言い淀みを除去して、アクティブなウィンドウに認識結果を直接入力します。Gemini が調整するので、単純な音声認識ではなく、文章がきれいになります。Gemini API料金は約0.3円/分です。

## 使用方法
インストールすると、スタートメニューに緑のマイクアイコンの Audio Transcription が追加になるので、起動すると、タスクトレイに常駐します。
`Ctrl + Shift + スペース` を押すと、録音が始まり、もう一度 `Ctrl + Shift + スペース` を押すと認識が終了します。
認識結果はアクティブなウィンドウに直接入力されます。

タスクトレイのアイコンを右クリックすると終了できます。

## インストール方法
https://github.com/yukoba/AudioTranscription/releases からインストールしてください。

Gemini を使用しているので API キーは https://aistudio.google.com/app/apikey から作成してください。
その API キーを環境変数 GEMINI_API_KEY に書くか、もしくは、以下の内容で `%USERPROFILE%\AudioTranscription.json` に書いてください。

```json
{
	"Gemini": {
		"ApiKey": "AAAAAAAAA"
	}
}
```

## ライセンス
アイコンは https://www.flaticon.com/free-icon/circle_14025057 より。ソースコードはMITライセンスです。
