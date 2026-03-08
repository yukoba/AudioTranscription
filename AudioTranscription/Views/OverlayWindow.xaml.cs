using System.Windows;

namespace AudioTranscription.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();

        // 画面の中央下部に配置する
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + workArea.Height - Height - 50; // 下から50px
    }

    public void SetStatus(string status)
    {
        StatusText.Text = status;
    }
}
