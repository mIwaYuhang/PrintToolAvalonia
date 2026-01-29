using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PrintToolAvalonia.ViewModels;
using PrintToolAvalonia.Views;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia;

public partial class App : Application
{
    /// <summary>
    /// 依赖注入服务提供者
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 加载应用配置
            _ = LoadConfigurationAsync();

            // 检查是否是调试模式
            var args = desktop.Args ?? Array.Empty<string>();
            
            if (args.Contains("--debug-settings"))
            {
                // 调试模式：直接打开设置对话框
                Console.WriteLine("=== 调试模式：打开设置对话框 ===");
                var settingsDialog = Services.GetRequiredService<SettingsDialog>();
                var settingsViewModel = Services.GetRequiredService<SettingsViewModel>();
                settingsViewModel.OwnerWindow = settingsDialog;
                settingsDialog.DataContext = settingsViewModel;
                desktop.MainWindow = settingsDialog;
            }
            else if (args.Contains("--debug-history"))
            {
                // 调试模式：直接打开历史记录对话框
                Console.WriteLine("=== 调试模式：打开历史记录对话框 ===");
                var historyDialog = Services.GetRequiredService<HistoryDialog>();
                var historyViewModel = Services.GetRequiredService<HistoryViewModel>();
                historyDialog.DataContext = historyViewModel;
                desktop.MainWindow = historyDialog;
            }
            else
            {
                // 正常模式：打开主窗口
                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 加载应用配置
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
        try
        {
            var configService = Services?.GetRequiredService<IConfigService>();
            if (configService != null)
            {
                var config = await configService.LoadAsync();
                Console.WriteLine("应用配置加载成功");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载应用配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // 注册 Services
        services.AddSingleton<IPrintService, WindowsPrintService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IConfigService, ConfigService>();

        // 注册 ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<EcoCodeViewModel>();

        // 注册 Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
        services.AddTransient<HistoryDialog>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}