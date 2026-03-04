using System.Windows;

namespace WhisperSpeechRecognition.Views
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();

            // 画面の中央下部に配置する
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Left + (workArea.Width - this.Width) / 2;
            this.Top = workArea.Top + workArea.Height - this.Height - 50; // 下から50px
        }

        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}
