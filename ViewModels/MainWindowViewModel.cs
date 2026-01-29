using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;
using PrintToolAvalonia.Views;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IPrintService _printService;
    private readonly IDatabaseService _databaseService;

    // ========== 文件列表 ==========
    
    /// <summary>
    /// 主单文件列表
    /// </summary>
    public ObservableCollection<ImportedFile> MainOrderFiles { get; } = new();
    
    /// <summary>
    /// 条码文件列表
    /// </summary>
    public ObservableCollection<ImportedFile> BarcodeFiles { get; } = new();

    // ========== 环保码 ==========
    
    /// <summary>
    /// 环保码列表
    /// </summary>
    public ObservableCollection<EcoCodeItem> EcoCodes { get; } = new();
    
    private EcoCodeItem? _selectedEcoCode;
    /// <summary>
    /// 选中的环保码
    /// </summary>
    public EcoCodeItem? SelectedEcoCode
    {
        get => _selectedEcoCode;
        set => SetProperty(ref _selectedEcoCode, value);
    }
    
    // ========== 英代欧代条码 ==========
    
    private bool _ukEuBarcodeEnabled;
    /// <summary>
    /// 是否启用英代欧代条码
    /// </summary>
    public bool UkEuBarcodeEnabled
    {
        get => _ukEuBarcodeEnabled;
        set
        {
            if (SetProperty(ref _ukEuBarcodeEnabled, value))
            {
                OnUkEuBarcodeEnabledChanged(value);
            }
        }
    }
    
    private int _ukEuBarcodeQuantity;
    /// <summary>
    /// 英代欧代条码数量
    /// </summary>
    public int UkEuBarcodeQuantity
    {
        get => _ukEuBarcodeQuantity;
        set => SetProperty(ref _ukEuBarcodeQuantity, value);
    }
    
    // ========== 环保码数量计算 ==========
    
    private int _calculatedEcoCodeQuantity;
    /// <summary>
    /// 计算的环保码数量（只读，自动计算）
    /// </summary>
    public int CalculatedEcoCodeQuantity
    {
        get => _calculatedEcoCodeQuantity;
        private set => SetProperty(ref _calculatedEcoCodeQuantity, value);
    }
    
    private int _customEcoCodeQuantity;
    /// <summary>
    /// 自定义环保码数量（用户可修改）
    /// </summary>
    public int CustomEcoCodeQuantity
    {
        get => _customEcoCodeQuantity;
        set => SetProperty(ref _customEcoCodeQuantity, value);
    }
    
    /// <summary>
    /// 主单总页数
    /// </summary>
    public int MainOrderPageCount => MainOrderFiles.Sum(f => f.PageCount);
    
    /// <summary>
    /// 条码总页数
    /// </summary>
    public int BarcodePageCount => BarcodeFiles.Sum(f => f.PageCount);

    // ========== 平台选择 ==========
    
    /// <summary>
    /// 可用平台列表
    /// </summary>
    public ObservableCollection<Platform> Platforms { get; } = new()
    {
        Platform.TEMU,
        Platform.SHEIN
    };
    
    private Platform _selectedPlatform = Platform.TEMU;
    /// <summary>
    /// 选中的平台
    /// </summary>
    public Platform SelectedPlatform
    {
        get => _selectedPlatform;
        set
        {
            if (SetProperty(ref _selectedPlatform, value))
            {
                OnSelectedPlatformChanged(value);
            }
        }
    }
    
    // ========== 统计信息 ==========
    
    /// <summary>
    /// 主单文件数量
    /// </summary>
    public int MainOrderCount => MainOrderFiles.Count;
    
    /// <summary>
    /// 条码文件数量
    /// </summary>
    public int BarcodeCount => BarcodeFiles.Count;

    // ========== 打印状态 ==========
    
    private bool _isPrinting;
    /// <summary>
    /// 是否正在打印
    /// </summary>
    public bool IsPrinting
    {
        get => _isPrinting;
        set => SetProperty(ref _isPrinting, value);
    }
    
    private int _printProgress;
    /// <summary>
    /// 打印进度（0-100）
    /// </summary>
    public int PrintProgress
    {
        get => _printProgress;
        set => SetProperty(ref _printProgress, value);
    }

    // ========== 命令 ==========
    
    public ICommand AddFilesCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand PreviewEcoCodeCommand { get; }
    
    // ========== 子 ViewModel ==========
    
    /// <summary>
    /// 环保码管理 ViewModel
    /// </summary>
    public EcoCodeViewModel EcoCodeViewModel { get; }

    public MainWindowViewModel(
        IFileService fileService,
        IPrintService printService,
        IDatabaseService databaseService,
        EcoCodeViewModel ecoCodeViewModel)
    {
        _fileService = fileService;
        _printService = printService;
        _databaseService = databaseService;
        EcoCodeViewModel = ecoCodeViewModel;

        // 初始化命令
        AddFilesCommand = new RelayCommand<string>(async (fileType) => await AddFilesAsync(fileType));
        RemoveFileCommand = new RelayCommand<ImportedFile>(RemoveFile);
        ClearAllCommand = new RelayCommand<string>(ClearAll);
        PrintCommand = new AsyncRelayCommand(PrintAsync);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenHistoryCommand = new RelayCommand(OpenHistory);
        PreviewEcoCodeCommand = new AsyncRelayCommand<EcoCodeItem>(PreviewEcoCodeAsync);

        // 监听文件列表变化
        MainOrderFiles.CollectionChanged += (s, e) => OnFilesChanged();
        BarcodeFiles.CollectionChanged += (s, e) => OnFilesChanged();
        
        // 监听环保码列表变化，同步到主窗口的环保码列表
        EcoCodeViewModel.EcoCodes.CollectionChanged += (s, e) =>
        {
            EcoCodes.Clear();
            foreach (var item in EcoCodeViewModel.EcoCodes)
            {
                EcoCodes.Add(item);
            }
        };

        // 加载数据
        _ = LoadDataAsync();
    }
    
    /// <summary>
    /// 文件列表变化时更新计算值
    /// </summary>
    private void OnFilesChanged()
    {
        // 通知统计属性变化
        OnPropertyChanged(nameof(MainOrderCount));
        OnPropertyChanged(nameof(BarcodeCount));
        OnPropertyChanged(nameof(MainOrderPageCount));
        OnPropertyChanged(nameof(BarcodePageCount));
        
        // 更新计算的数量
        UpdateCalculatedQuantities();
    }
    
    /// <summary>
    /// 平台变化时更新计算值
    /// </summary>
    private void OnSelectedPlatformChanged(Platform value)
    {
        UpdateCalculatedQuantities();
    }
    
    /// <summary>
    /// 英代欧代启用状态变化
    /// </summary>
    private void OnUkEuBarcodeEnabledChanged(bool value)
    {
        if (value)
        {
            // 启用时，默认数量等于环保码数量
            UkEuBarcodeQuantity = CustomEcoCodeQuantity;
        }
        else
        {
            // 禁用时，数量设为 0
            UkEuBarcodeQuantity = 0;
        }
    }
    
    /// <summary>
    /// 计算环保码数量
    /// TEMU 平台：BarcodePageCount - MainOrderPageCount + 1（最小为 0）
    /// SHEIN 平台：BarcodePageCount
    /// </summary>
    private int CalculateEcoCodeQuantity()
    {
        if (SelectedPlatform == Platform.SHEIN)
        {
            return BarcodePageCount;
        }
        else // TEMU
        {
            return Math.Max(0, BarcodePageCount - MainOrderPageCount + 1);
        }
    }
    
    /// <summary>
    /// 更新计算的数量
    /// </summary>
    private void UpdateCalculatedQuantities()
    {
        // 更新环保码计算数量
        var calculated = CalculateEcoCodeQuantity();
        CalculatedEcoCodeQuantity = calculated;
        CustomEcoCodeQuantity = calculated;
        
        // 如果英代欧代启用，同步数量
        if (UkEuBarcodeEnabled)
        {
            UkEuBarcodeQuantity = calculated;
        }
    }

    /// <summary>
    /// 加载初始数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            // 加载环保码列表
            var ecoCodes = await _databaseService.GetAllEcoCodesAsync();
            EcoCodes.Clear();
            foreach (var item in ecoCodes)
            {
                EcoCodes.Add(item);
            }

            // 加载上次使用的平台
            var config = await _databaseService.GetConfigAsync();
            SelectedPlatform = config.LastPlatform;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    private async Task AddFilesAsync(string? fileType)
    {
        try
        {
            var files = await _fileService.OpenFileDialogAsync("PDF Files|*.pdf");
            
            foreach (var filePath in files)
            {
                // 验证 PDF
                var validation = await _fileService.ValidatePdfAsync(filePath);
                
                if (!validation.IsValid)
                {
                    Console.WriteLine($"文件验证失败: {validation.Error}");
                    continue;
                }

                var importedFile = new ImportedFile
                {
                    Name = System.IO.Path.GetFileName(filePath),
                    Path = filePath,
                    PageCount = validation.PageCount
                };

                // 根据文件类型添加到对应列表
                if (fileType == "MainOrder")
                {
                    MainOrderFiles.Add(importedFile);
                }
                else if (fileType == "Barcode")
                {
                    BarcodeFiles.Add(importedFile);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加文件（从拖放操作）
    /// </summary>
    public async Task AddFilesAsync(string[] filePaths, string? fileType)
    {
        try
        {
            foreach (var filePath in filePaths)
            {
                // 验证 PDF
                var validation = await _fileService.ValidatePdfAsync(filePath);
                
                if (!validation.IsValid)
                {
                    Console.WriteLine($"文件验证失败: {validation.Error}");
                    continue;
                }

                var importedFile = new ImportedFile
                {
                    Name = System.IO.Path.GetFileName(filePath),
                    Path = filePath,
                    PageCount = validation.PageCount
                };

                // 根据文件类型添加到对应列表
                if (fileType == "MainOrder")
                {
                    MainOrderFiles.Add(importedFile);
                }
                else if (fileType == "Barcode")
                {
                    BarcodeFiles.Add(importedFile);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除单个文件
    /// </summary>
    private void RemoveFile(ImportedFile? file)
    {
        if (file == null) return;

        MainOrderFiles.Remove(file);
        BarcodeFiles.Remove(file);
    }

    /// <summary>
    /// 清空所有文件
    /// </summary>
    private void ClearAll(string? fileType)
    {
        if (fileType == "MainOrder")
        {
            MainOrderFiles.Clear();
        }
        else if (fileType == "Barcode")
        {
            BarcodeFiles.Clear();
        }
        else
        {
            MainOrderFiles.Clear();
            BarcodeFiles.Clear();
        }
    }

    /// <summary>
    /// 执行打印
    /// </summary>
    private async Task PrintAsync()
    {
        if (IsPrinting) return;

        try
        {
            // 验证是否有文件
            if (MainOrderFiles.Count == 0 && BarcodeFiles.Count == 0)
            {
                await ShowErrorAsync("请先添加要打印的文件");
                return;
            }

            IsPrinting = true;
            PrintProgress = 0;

            // 构建打印任务列表
            var jobs = new List<PrintJob>();
            var config = await _databaseService.GetConfigAsync();

            // 验证打印机配置
            if (!ValidatePrinterConfig(config))
            {
                await ShowErrorAsync("请先在设置中配置打印机");
                return;
            }

            // 添加主单打印任务
            foreach (var file in MainOrderFiles)
            {
                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = file.Path,
                        PrinterName = config.MainOrderPrinter.PrinterName,
                        PaperWidthMm = config.MainOrderPrinter.PaperWidthMm,
                        PaperHeightMm = config.MainOrderPrinter.PaperHeightMm,
                        Copies = 1
                    },
                    Description = $"主单: {file.Name}"
                });
            }

            // 添加条码打印任务
            foreach (var file in BarcodeFiles)
            {
                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = file.Path,
                        PrinterName = config.BarcodePrinter.PrinterName,
                        PaperWidthMm = config.BarcodePrinter.PaperWidthMm,
                        PaperHeightMm = config.BarcodePrinter.PaperHeightMm,
                        Copies = 1
                    },
                    Description = $"条码: {file.Name}"
                });
            }

            // 添加环保码打印任务（使用自定义数量）
            if (SelectedEcoCode != null && CustomEcoCodeQuantity > 0)
            {
                // 获取环保码文件路径
                var configService = App.Services?.GetRequiredService<IConfigService>();
                var ecoCodePath = System.IO.Path.Combine(
                    configService?.GetAppDataPath() ?? "",
                    "eco_codes",
                    SelectedEcoCode.FileName
                );

                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = ecoCodePath,
                        PrinterName = config.EcoCodePrinter.PrinterName,
                        PaperWidthMm = config.EcoCodePrinter.PaperWidthMm,
                        PaperHeightMm = config.EcoCodePrinter.PaperHeightMm,
                        Copies = CustomEcoCodeQuantity
                    },
                    Description = $"环保码: {SelectedEcoCode.Name}"
                });
            }
            
            // 添加英代欧代条码打印任务（使用条码打印机配置）
            if (UkEuBarcodeEnabled && UkEuBarcodeQuantity > 0)
            {
                var ukEuBarcodePath = _printService.GetBuiltinResourcePath("uk_eu_barcode.pdf");
                
                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = ukEuBarcodePath,
                        PrinterName = config.BarcodePrinter.PrinterName,
                        PaperWidthMm = config.BarcodePrinter.PaperWidthMm,
                        PaperHeightMm = config.BarcodePrinter.PaperHeightMm,
                        Copies = UkEuBarcodeQuantity
                    },
                    Description = $"英代欧代条码"
                });
            }

            // 执行批量打印
            var progress = new Progress<int>(value =>
            {
                PrintProgress = (int)((double)value / jobs.Count * 100);
            });

            var result = await _printService.PrintBatchAsync(jobs, progress);

            // 创建打印记录
            var record = new PrintRecord
            {
                Platform = SelectedPlatform,
                MainOrderFiles = MainOrderFiles.Select(f => f.Name).ToList(),
                BarcodeFiles = BarcodeFiles.Select(f => f.Name).ToList(),
                EcoCodeName = SelectedEcoCode?.Name,
                MainOrderCount = MainOrderFiles.Count,
                BarcodeCount = BarcodeFiles.Count,
                EcoCodeCount = CustomEcoCodeQuantity,
                Status = result.FailedCount == 0 ? PrintStatus.Success : PrintStatus.Failed
            };

            await _databaseService.AddRecordAsync(record);

            // 保存当前平台选择
            config.LastPlatform = SelectedPlatform;
            await _databaseService.SaveConfigAsync(config);

            // 显示打印结果
            if (result.FailedCount == 0)
            {
                await ShowInfoAsync($"打印完成！成功打印 {result.SuccessCount} 个任务");
            }
            else
            {
                var errorMsg = $"打印完成：成功 {result.SuccessCount} 个，失败 {result.FailedCount} 个\n\n失败详情：\n";
                foreach (var (job, error) in result.FailedJobs.Take(5))
                {
                    errorMsg += $"- {job.Description}: {error}\n";
                }
                if (result.FailedJobs.Count > 5)
                {
                    errorMsg += $"... 还有 {result.FailedJobs.Count - 5} 个失败任务";
                }
                await ShowErrorAsync(errorMsg);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"打印失败: {ex.Message}");
        }
        finally
        {
            IsPrinting = false;
            PrintProgress = 0;
        }
    }

    /// <summary>
    /// 验证打印机配置
    /// </summary>
    private bool ValidatePrinterConfig(AppConfig config)
    {
        if (MainOrderFiles.Count > 0 && string.IsNullOrEmpty(config.MainOrderPrinter.PrinterName))
        {
            return false;
        }
        
        if (BarcodeFiles.Count > 0 && string.IsNullOrEmpty(config.BarcodePrinter.PrinterName))
        {
            return false;
        }
        
        if (SelectedEcoCode != null && string.IsNullOrEmpty(config.EcoCodePrinter.PrinterName))
        {
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private async Task ShowErrorAsync(string message)
    {
        // TODO: 使用 Avalonia 消息框
        Console.WriteLine($"错误: {message}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    private async Task ShowInfoAsync(string message)
    {
        // TODO: 使用 Avalonia 消息框
        Console.WriteLine($"信息: {message}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 打开设置对话框
    /// </summary>
    private void OpenSettings()
    {
        try
        {
            var settingsDialog = App.Services?.GetRequiredService<SettingsDialog>();
            var settingsViewModel = App.Services?.GetRequiredService<SettingsViewModel>();
            
            if (settingsDialog != null && settingsViewModel != null)
            {
                settingsDialog.DataContext = settingsViewModel;
                _ = settingsDialog.ShowDialog(GetMainWindow());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开设置对话框失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开历史对话框
    /// </summary>
    private void OpenHistory()
    {
        try
        {
            var historyDialog = App.Services?.GetRequiredService<HistoryDialog>();
            var historyViewModel = App.Services?.GetRequiredService<HistoryViewModel>();
            
            if (historyDialog != null && historyViewModel != null)
            {
                historyDialog.DataContext = historyViewModel;
                _ = historyDialog.ShowDialog(GetMainWindow());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开历史对话框失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取主窗口
    /// </summary>
    private Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime 
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    /// <summary>
    /// 预览环保码
    /// </summary>
    private async Task PreviewEcoCodeAsync(EcoCodeItem? ecoCode)
    {
        if (ecoCode == null) return;

        try
        {
            // 获取环保码文件路径
            var configService = App.Services?.GetRequiredService<IConfigService>();
            var ecoCodePath = System.IO.Path.Combine(
                configService?.GetAppDataPath() ?? "",
                "eco_codes",
                ecoCode.FileName
            );

            if (!System.IO.File.Exists(ecoCodePath))
            {
                await ShowErrorAsync($"环保码文件不存在: {ecoCode.Name}");
                return;
            }

            // 创建预览对话框
            var previewDialog = new Views.PdfPreviewDialog();
            var previewViewModel = new PdfPreviewViewModel();
            
            // 初始化 ViewModel
            previewViewModel.Initialize(ecoCodePath, ecoCode.Name);
            
            // 订阅关闭事件
            previewViewModel.RequestClose += (s, e) => previewDialog.Close();
            
            // 设置 DataContext
            previewDialog.DataContext = previewViewModel;
            
            // 显示对话框
            await previewDialog.ShowDialog(GetMainWindow());
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"预览失败: {ex.Message}");
        }
    }
}
