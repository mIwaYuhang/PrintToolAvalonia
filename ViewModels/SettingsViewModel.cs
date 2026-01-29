using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 打印机类型枚举
/// </summary>
public enum PrinterType
{
    MainOrder,
    Barcode,
    EcoCode
}

/// <summary>
/// 设置对话框 ViewModel
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IPrintService _printService;
    private readonly IDatabaseService _databaseService;
    private readonly IFileService _fileService;
    
    /// <summary>
    /// 父窗口引用（用于显示对话框）
    /// </summary>
    public Avalonia.Controls.Window? OwnerWindow { get; set; }

    // ========== 打印机列表 ==========
    
    /// <summary>
    /// 可用打印机列表
    /// </summary>
    public ObservableCollection<string> AvailablePrinters { get; } = new();

    // ========== 打印机配置 ==========
    
    private PrinterConfig _mainOrderPrinter = new();
    /// <summary>
    /// 主单打印机配置
    /// </summary>
    public PrinterConfig MainOrderPrinter
    {
        get => _mainOrderPrinter;
        set => SetProperty(ref _mainOrderPrinter, value);
    }
    
    private PrinterConfig _barcodePrinter = new();
    /// <summary>
    /// 条码打印机配置
    /// </summary>
    public PrinterConfig BarcodePrinter
    {
        get => _barcodePrinter;
        set => SetProperty(ref _barcodePrinter, value);
    }
    
    private PrinterConfig _ecoCodePrinter = new();
    /// <summary>
    /// 环保码打印机配置
    /// </summary>
    public PrinterConfig EcoCodePrinter
    {
        get => _ecoCodePrinter;
        set => SetProperty(ref _ecoCodePrinter, value);
    }

    // ========== OCR区域配置 ==========
    
    private OcrRegion _trackingNumberRegion = new();
    /// <summary>
    /// 快递单号识别区域
    /// </summary>
    public OcrRegion TrackingNumberRegion
    {
        get => _trackingNumberRegion;
        set => SetProperty(ref _trackingNumberRegion, value);
    }
    
    private OcrRegion _packageCountRegion = new();
    /// <summary>
    /// 件数识别区域
    /// </summary>
    public OcrRegion PackageCountRegion
    {
        get => _packageCountRegion;
        set => SetProperty(ref _packageCountRegion, value);
    }

    // ========== 测试状态 ==========
    
    private Dictionary<PrinterType, bool> _isTesting = new()
    {
        { PrinterType.MainOrder, false },
        { PrinterType.Barcode, false },
        { PrinterType.EcoCode, false }
    };
    
    /// <summary>
    /// 主单打印机是否正在测试
    /// </summary>
    public bool IsTestingMainOrder
    {
        get => _isTesting[PrinterType.MainOrder];
        private set
        {
            _isTesting[PrinterType.MainOrder] = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 条码打印机是否正在测试
    /// </summary>
    public bool IsTestingBarcode
    {
        get => _isTesting[PrinterType.Barcode];
        private set
        {
            _isTesting[PrinterType.Barcode] = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 环保码打印机是否正在测试
    /// </summary>
    public bool IsTestingEcoCode
    {
        get => _isTesting[PrinterType.EcoCode];
        private set
        {
            _isTesting[PrinterType.EcoCode] = value;
            OnPropertyChanged();
        }
    }

    // ========== 命令 ==========
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshPrintersCommand { get; }
    public ICommand TestPrintCommand { get; }
    public ICommand ResetOcrRegionsCommand { get; }
    public ICommand OpenOcrConfigCommand { get; }

    // ========== 事件 ==========
    
    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event EventHandler? RequestClose;

    public SettingsViewModel(
        IPrintService printService,
        IDatabaseService databaseService,
        IFileService fileService)
    {
        _printService = printService;
        _databaseService = databaseService;
        _fileService = fileService;

        // 初始化命令
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        RefreshPrintersCommand = new AsyncRelayCommand(RefreshPrintersAsync);
        TestPrintCommand = new AsyncRelayCommand<PrinterType>(TestPrintAsync);
        ResetOcrRegionsCommand = new RelayCommand(ResetOcrRegions);
        OpenOcrConfigCommand = new AsyncRelayCommand(OpenOcrConfigAsync);

        // 加载数据
        _ = LoadDataAsync();
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            // 加载打印机列表
            await RefreshPrintersAsync();

            // 加载配置
            var config = await _databaseService.GetConfigAsync();
            MainOrderPrinter = new PrinterConfig
            {
                PrinterName = config.MainOrderPrinter.PrinterName,
                PaperWidthMm = config.MainOrderPrinter.PaperWidthMm,
                PaperHeightMm = config.MainOrderPrinter.PaperHeightMm
            };
            
            BarcodePrinter = new PrinterConfig
            {
                PrinterName = config.BarcodePrinter.PrinterName,
                PaperWidthMm = config.BarcodePrinter.PaperWidthMm,
                PaperHeightMm = config.BarcodePrinter.PaperHeightMm
            };
            
            EcoCodePrinter = new PrinterConfig
            {
                PrinterName = config.EcoCodePrinter.PrinterName,
                PaperWidthMm = config.EcoCodePrinter.PaperWidthMm,
                PaperHeightMm = config.EcoCodePrinter.PaperHeightMm
            };
            
            // 加载OCR区域配置
            TrackingNumberRegion = new OcrRegion
            {
                X = config.TrackingNumberRegion.X,
                Y = config.TrackingNumberRegion.Y,
                Width = config.TrackingNumberRegion.Width,
                Height = config.TrackingNumberRegion.Height
            };
            
            PackageCountRegion = new OcrRegion
            {
                X = config.PackageCountRegion.X,
                Y = config.PackageCountRegion.Y,
                Width = config.PackageCountRegion.Width,
                Height = config.PackageCountRegion.Height
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新打印机列表
    /// </summary>
    private async Task RefreshPrintersAsync()
    {
        try
        {
            var printers = await _printService.GetPrintersAsync();
            
            AvailablePrinters.Clear();
            foreach (var printer in printers)
            {
                AvailablePrinters.Add(printer.Name);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"刷新打印机列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试打印
    /// </summary>
    /// <param name="printerType">打印机类型</param>
    private async Task TestPrintAsync(PrinterType printerType)
    {
        try
        {
            // 设置测试状态
            SetTestingState(printerType, true);

            // 获取对应的打印机配置
            var config = GetPrinterConfig(printerType);
            
            // 验证打印机是否已配置
            if (string.IsNullOrEmpty(config.PrinterName))
            {
                await ShowErrorAsync("请先选择打印机");
                return;
            }

            // 验证纸张尺寸
            if (!IsValidPaperSize(config))
            {
                await ShowErrorAsync("纸张尺寸无效，请输入 10-500mm 之间的值");
                return;
            }

            string? testFilePath = null;

            // 根据打印机类型选择测试文件
            if (printerType == PrinterType.EcoCode)
            {
                // 环保码打印机：使用当前选中的环保码文件
                var ecoCodes = await _databaseService.GetAllEcoCodesAsync();
                if (ecoCodes.Count == 0)
                {
                    await ShowErrorAsync("没有可用的环保码文件，请先在环保码管理中添加");
                    return;
                }

                // 使用第一个环保码文件
                var ecoCode = ecoCodes.First();
                var configService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<IConfigService>(App.Services!);
                testFilePath = System.IO.Path.Combine(
                    configService.GetAppDataPath(),
                    "eco_codes",
                    ecoCode.FileName
                );

                if (!System.IO.File.Exists(testFilePath))
                {
                    await ShowErrorAsync($"环保码文件不存在: {ecoCode.Name}");
                    return;
                }
            }
            else
            {
                // 主单和条码打印机：打开文件选择对话框
                var files = await _fileService.OpenFileDialogAsync("PDF Files|*.pdf", OwnerWindow);
                if (files.Length == 0)
                {
                    // 用户取消选择，不显示错误
                    return;
                }

                testFilePath = files[0];
            }

            Console.WriteLine($"开始测试打印: 打印机={config.PrinterName}, 文件={testFilePath}");

            // 执行测试打印（只打印第一页）
            var result = await _printService.PrintPdfAsync(new PrintOptions
            {
                FilePath = testFilePath,
                PrinterName = config.PrinterName,
                PaperWidthMm = config.PaperWidthMm,
                PaperHeightMm = config.PaperHeightMm,
                Copies = 1
            });

            Console.WriteLine($"测试打印结果: Success={result.Success}, Error={result.Error}");

            // 显示测试结果
            if (result.Success)
            {
                await ShowInfoAsync($"测试打印成功！\n打印机: {config.PrinterName}\n纸张: {config.PaperWidthMm}x{config.PaperHeightMm}mm");
            }
            else
            {
                await ShowErrorAsync($"测试打印失败: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试打印异常: {ex}");
            await ShowErrorAsync($"测试打印失败: {ex.Message}");
        }
        finally
        {
            // 重置测试状态
            SetTestingState(printerType, false);
        }
    }

    /// <summary>
    /// 获取打印机配置
    /// </summary>
    private PrinterConfig GetPrinterConfig(PrinterType printerType)
    {
        return printerType switch
        {
            PrinterType.MainOrder => MainOrderPrinter,
            PrinterType.Barcode => BarcodePrinter,
            PrinterType.EcoCode => EcoCodePrinter,
            _ => throw new ArgumentException($"未知的打印机类型: {printerType}")
        };
    }

    /// <summary>
    /// 设置测试状态
    /// </summary>
    private void SetTestingState(PrinterType printerType, bool isTesting)
    {
        switch (printerType)
        {
            case PrinterType.MainOrder:
                IsTestingMainOrder = isTesting;
                break;
            case PrinterType.Barcode:
                IsTestingBarcode = isTesting;
                break;
            case PrinterType.EcoCode:
                IsTestingEcoCode = isTesting;
                break;
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            // 验证配置
            if (!ValidateConfig())
            {
                Console.WriteLine("配置验证失败");
                return;
            }

            // 加载现有配置
            var config = await _databaseService.GetConfigAsync();
            
            // 更新配置
            config.MainOrderPrinter = new PrinterConfig
            {
                PrinterName = MainOrderPrinter.PrinterName,
                PaperWidthMm = MainOrderPrinter.PaperWidthMm,
                PaperHeightMm = MainOrderPrinter.PaperHeightMm
            };
            
            config.BarcodePrinter = new PrinterConfig
            {
                PrinterName = BarcodePrinter.PrinterName,
                PaperWidthMm = BarcodePrinter.PaperWidthMm,
                PaperHeightMm = BarcodePrinter.PaperHeightMm
            };
            
            config.EcoCodePrinter = new PrinterConfig
            {
                PrinterName = EcoCodePrinter.PrinterName,
                PaperWidthMm = EcoCodePrinter.PaperWidthMm,
                PaperHeightMm = EcoCodePrinter.PaperHeightMm
            };
            
            // 更新OCR区域配置
            config.TrackingNumberRegion = new OcrRegion
            {
                X = TrackingNumberRegion.X,
                Y = TrackingNumberRegion.Y,
                Width = TrackingNumberRegion.Width,
                Height = TrackingNumberRegion.Height
            };
            
            config.PackageCountRegion = new OcrRegion
            {
                X = PackageCountRegion.X,
                Y = PackageCountRegion.Y,
                Width = PackageCountRegion.Width,
                Height = PackageCountRegion.Height
            };

            // 保存到数据库
            await _databaseService.SaveConfigAsync(config);

            Console.WriteLine("配置保存成功");
            
            // 关闭对话框
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 取消并关闭对话框
    /// </summary>
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 恢复OCR区域默认值
    /// </summary>
    private void ResetOcrRegions()
    {
        // 快递单号识别区域（左下角）
        TrackingNumberRegion = new OcrRegion
        {
            X = 0.05f,
            Y = 0.85f,
            Width = 0.5f,
            Height = 0.08f
        };
        
        // 件数识别区域（右侧中间）
        PackageCountRegion = new OcrRegion
        {
            X = 0.7f,
            Y = 0.45f,
            Width = 0.25f,
            Height = 0.15f
        };
    }
    
    /// <summary>
    /// 打开OCR可视化配置对话框
    /// </summary>
    private async Task OpenOcrConfigAsync()
    {
        try
        {
            var dialog = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<Views.OcrRegionConfigDialog>(App.Services!);
            var viewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<OcrRegionConfigViewModel>(App.Services!);
            
            viewModel.OwnerWindow = dialog;
            dialog.DataContext = viewModel;
            
            await dialog.ShowDialog(OwnerWindow!);
            
            // 对话框关闭后，重新加载配置以更新显示
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开OCR配置对话框失败: {ex.Message}");
            await ShowErrorAsync($"打开配置对话框失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    private bool ValidateConfig()
    {
        // 验证纸张尺寸在合理范围内（10-500mm）
        if (!IsValidPaperSize(MainOrderPrinter) ||
            !IsValidPaperSize(BarcodePrinter) ||
            !IsValidPaperSize(EcoCodePrinter))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证纸张尺寸
    /// </summary>
    private bool IsValidPaperSize(PrinterConfig config)
    {
        return config.PaperWidthMm >= 10 && config.PaperWidthMm <= 500 &&
               config.PaperHeightMm >= 10 && config.PaperHeightMm <= 500;
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private async Task ShowErrorAsync(string message)
    {
        await Views.MessageDialog.ShowErrorAsync(OwnerWindow, message);
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    private async Task ShowInfoAsync(string message)
    {
        await Views.MessageDialog.ShowInfoAsync(OwnerWindow, message);
    }
}
