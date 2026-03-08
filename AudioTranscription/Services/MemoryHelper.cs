using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AudioTranscription.Services;

public static class MemoryHelper
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

    public static void ReleaseMemory()
    {
        // 強制的にガベージコレクションを実行
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            try
            {
                // ワーキングセット（物理メモリ）をページファイルに退避して解放
                using var process = Process.GetCurrentProcess();
                SetProcessWorkingSetSize(process.Handle, unchecked((nuint)(-1)), unchecked((nuint)(-1)));
            }
            catch
            {
                // エラー時は無視
            }
    }
}
