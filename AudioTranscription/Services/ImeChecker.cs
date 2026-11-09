using System.Runtime.InteropServices;

namespace AudioTranscription.Services
{
    public static class ImeChecker
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_IME_CONTROL = 0x0283;
        private const int IMC_GETOPENSTATUS = 0x0005;
        private const int IMC_GETCONVERSIONMODE = 0x0001; // 追加：変換モード取得

        private const int IME_CMODE_NATIVE = 0x0001; // 1なら「かな（日本語）」、0なら「半角英数」

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public System.Drawing.Rectangle rcCaret;
        }

        public static bool IsImeOpen()
        {
            IntPtr foregroundWnd = GetForegroundWindow();
            if (foregroundWnd == IntPtr.Zero) return false;

            uint threadId = GetWindowThreadProcessId(foregroundWnd, IntPtr.Zero);
            GUITHREADINFO guiInfo = new GUITHREADINFO();
            guiInfo.cbSize = Marshal.SizeOf(guiInfo);

            IntPtr targetWnd = foregroundWnd;
            if (GetGUIThreadInfo(threadId, ref guiInfo) && guiInfo.hwndFocus != IntPtr.Zero)
            {
                targetWnd = guiInfo.hwndFocus;
            }

            IntPtr imeWnd = ImmGetDefaultIMEWnd(targetWnd);
            if (imeWnd == IntPtr.Zero) return false;

            // 1. まずIME自体がOpenか確認
            long isOpen = SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero).ToInt64();
            if (isOpen == 0)
            {
                return false; // 完全にオフなら false
            }

            // 2. Openだった場合、さらに変換モードを確認する（Copilot Keyboard対策）
            long convMode = SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, IntPtr.Zero).ToInt64();

            // 最下位ビット(IME_CMODE_NATIVE)が立っていれば「日本語入力状態（あ）」
            return (convMode & IME_CMODE_NATIVE) != 0;
        }
    }
}
