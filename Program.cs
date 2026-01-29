using Avalonia;
using System;
using System.Linq;

namespace PrintToolAvalonia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 检查是否有调试参数
        if (args.Contains("--debug"))
        {
            Console.WriteLine("=== 调试模式启动 ===");
            Console.WriteLine("可用的调试选项:");
            Console.WriteLine("  --debug-settings  : 直接打开设置对话框");
            Console.WriteLine("  --debug-history   : 直接打开历史记录对话框");
            Console.WriteLine("  --debug-ecocode   : 直接打开环保码管理对话框");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
