using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Avalonia;

namespace NHLauncher.Desktop;

sealed class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        _mutex = new Mutex(true, "NHLauncher_Global_Mutex_ID", out bool createdNew);

        if (!createdNew)
        {
            // 发现重复，去唤醒旧的
            NotifyOldInstance();
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _mutex?.ReleaseMutex();
        }
    }

    private static void NotifyOldInstance()
    {
        try
        {
            // 连接到旧实例开启的管道
            using var client = new NamedPipeClientStream(".", "NHLauncher_SingleInstance_Pipe", PipeDirection.Out);
            client.Connect(1000); // 1秒超时
            using var writer = new StreamWriter(client);
            writer.Write("WAKEUP");
            writer.Flush();
        }
        catch
        {
            // 如果管道连不上（可能旧实例卡死了），再考虑弹窗提示
            if (OperatingSystem.IsWindows())
            {
                MessageBox(IntPtr.Zero, "程序正在运行中，但旧实例没有响应。", "提示", 0x40);
            }
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}