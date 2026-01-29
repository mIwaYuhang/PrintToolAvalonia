using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 单页打印视图模型
/// </summary>
public partial class SinglePagePrintViewModel : ViewModelBase
{
    private readonly IPdfRenderService _pdfRenderService;
    private readonly IOcrService _ocrService;
    private readonly IPrintService _printService;
    private readonly IDatabaseService _databaseService;
    private readonly IConfigService _configService;
    private readonly IImageMatchService _imageMatchService;
    private readonly IBarcodeGroupService _barcodeGroupService;
    
    private string _pdfFilePath = string.Empty;
    private string? _barcodePdfFilePath;
    private List<BarcodeGroup> _barcodeGroups = new();
    
    /// <summary>
    /// 所属窗口（用于显示对话框）
    /// </summary>
    public Avalonia.Controls.Window? OwnerWindow { get; set; }
    
    [ObservableProperty]
    private int _currentPage = 1;
    
    [ObservableProperty]
    private int _totalPages;
    
    [ObservableProperty]
    private Bitmap? _currentPageImage;
    
    [ObservableProperty]
    private string _trackingNumber = "未识别";
    
    [ObservableProperty]
    private string _packageCount = "未识别";
    
    [ObservableProperty]
    private ObservableCollection<EcoCodeItem> _ecoCodes = new();
    
    [ObservableProperty]
    private EcoCodeItem? _selectedEcoCode;
    
    [ObservableProperty]
    private int _ecoCodeQuantity;
    
    [ObservableProperty]
    private bool _ukEuBarcodeEnabled;
    
    [ObservableProperty]
    private int _ukEuBarcodeQuantity;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _isPrinting;
    
    /// <summary>
    /// 选中的条码分组
    /// </summary>
    [ObservableProperty]
    private BarcodeGroup? _selectedBarcodeGroup;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public SinglePagePrintViewModel(
        IPdfRenderService pdfRenderService,
        IOcrService ocrService,
        IPrintService printService,
        IDatabaseService databaseService,
        IConfigService configService,
        IImageMatchService imageMatchService,
        IBarcodeGroupService barcodeGroupService)
    {
        _pdfRenderService = pdfRenderService;
        _ocrService = ocrService;
        _printService = printService;
        _databaseService = databaseService;
        _configService = configService;
        _imageMatchService = imageMatchService;
        _barcodeGroupService = barcodeGroupService;
    }
    
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="pdfFilePath">PDF文件路径</param>
    /// <param name="ecoCodes">环保码列表</param>
    /// <param name="barcodePdfPath">条码PDF文件路径（可选）</param>
    public async Task InitializeAsync(string pdfFilePath, ObservableCollection<EcoCodeItem> ecoCodes, string? barcodePdfPath = null)
    {
        _pdfFilePath = pdfFilePath;
        _barcodePdfFilePath = barcodePdfPath;
        
        // 初始化环保码列表
        EcoCodes = ecoCodes;
        
        // 获取总页数
        TotalPages = await _pdfRenderService.GetPageCountAsync(pdfFilePath);
        
        // 加载第一页
        CurrentPage = 1;
        await LoadPageAsync(CurrentPage);
        
        // 如果提供了条码PDF路径，初始化条码分组（异步，不阻塞UI）
        if (!string.IsNullOrEmpty(_barcodePdfFilePath))
        {
            _ = InitializeBarcodeGroupsAsync();
        }
    }
    
    /// <summary>
    /// 初始化条码分组
    /// </summary>
    private async Task InitializeBarcodeGroupsAsync()
    {
        try
        {
            // 验证条码PDF文件是否存在
            if (string.IsNullOrEmpty(_barcodePdfFilePath) || !System.IO.File.Exists(_barcodePdfFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"条码PDF文件不存在: {_barcodePdfFilePath}");
                return;
            }
            
            // 加载分隔符模板
            var templatePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "Resources", 
                "separator_template.png"
            );
            
            if (!System.IO.File.Exists(templatePath))
            {
                System.Diagnostics.Debug.WriteLine($"分隔符模板不存在: {templatePath}");
                return;
            }
            
            _imageMatchService.LoadTemplate(templatePath);
            
            // 扫描分隔符
            System.Diagnostics.Debug.WriteLine($"开始扫描条码PDF文件: {_barcodePdfFilePath}");
            var separatorPages = await _imageMatchService.ScanSeparatorsAsync(_barcodePdfFilePath);
            System.Diagnostics.Debug.WriteLine($"扫描完成，找到 {separatorPages.Count} 个分隔符页面: {string.Join(", ", separatorPages)}");
            
            // 创建分组
            _barcodeGroups = await _barcodeGroupService.CreateGroupsAsync(_barcodePdfFilePath, separatorPages);
            
            System.Diagnostics.Debug.WriteLine($"成功创建 {_barcodeGroups.Count} 个条码分组");
            for (int i = 0; i < _barcodeGroups.Count; i++)
            {
                var group = _barcodeGroups[i];
                System.Diagnostics.Debug.WriteLine($"  分组 {i + 1}: 第{group.StartPage}-{group.EndPage}页，共{group.BarcodeCount}个条码");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化条码分组失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 加载指定页面
    /// </summary>
    private async Task LoadPageAsync(int pageNumber)
    {
        try
        {
            IsLoading = true;
            
            // 清除之前选择的条码分组
            ClearBarcodeGroupSelection();
            
            // 渲染页面
            CurrentPageImage = await _pdfRenderService.RenderPageAsync(_pdfFilePath, pageNumber);
            
            // 识别页面信息
            if (CurrentPageImage != null)
            {
                await RecognizePageInfoAsync(CurrentPageImage);
                
                // 自动填充打印数量
                AutoFillPrintQuantities();
            }
        }
        catch (Exception ex)
        {
            TrackingNumber = "加载失败";
            PackageCount = "加载失败";
            System.Diagnostics.Debug.WriteLine($"加载页面失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 清除条码分组选择
    /// </summary>
    private void ClearBarcodeGroupSelection()
    {
        SelectedBarcodeGroup = null;
        
        // 重置英代欧代条码数量为识别到的件数
        // 注意：此时PackageCount可能还未更新，所以在AutoFillPrintQuantities中会重新设置
    }
    
    /// <summary>
    /// 识别当前页面信息
    /// </summary>
    private async Task RecognizePageInfoAsync(Bitmap image)
    {
        try
        {
            // 并行识别快递单号和件数
            var trackingNumberTask = _ocrService.RecognizeTrackingNumberAsync(image);
            var packageCountTask = _ocrService.RecognizePackageCountAsync(image);
            
            await Task.WhenAll(trackingNumberTask, packageCountTask);
            
            // 更新识别结果
            TrackingNumber = trackingNumberTask.Result;
            PackageCount = packageCountTask.Result;
        }
        catch (Exception ex)
        {
            TrackingNumber = "识别失败";
            PackageCount = "识别失败";
            System.Diagnostics.Debug.WriteLine($"识别页面信息失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 根据识别到的件数自动填充打印数量
    /// </summary>
    private void AutoFillPrintQuantities()
    {
        // 从PackageCount中提取数字
        var number = ExtractNumber(PackageCount);
        
        if (int.TryParse(number, out int count) && count > 0)
        {
            // 自动填充环保码数量
            EcoCodeQuantity = count;
            
            // 如果英代欧代条码已启用，也自动填充
            if (UkEuBarcodeEnabled)
            {
                UkEuBarcodeQuantity = count;
            }
        }
    }
    
    /// <summary>
    /// 从文本中提取数字
    /// </summary>
    private string ExtractNumber(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
        return match.Success ? match.Value : "0";
    }
    
    /// <summary>
    /// 上一页命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadPageAsync(CurrentPage);
        }
    }
    
    /// <summary>
    /// 是否可以上一页
    /// </summary>
    private bool CanGoPreviousPage()
    {
        return CurrentPage > 1 && !IsLoading && !IsPrinting;
    }
    
    /// <summary>
    /// 下一页命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadPageAsync(CurrentPage);
        }
    }
    
    /// <summary>
    /// 是否可以下一页
    /// </summary>
    private bool CanGoNextPage()
    {
        return CurrentPage < TotalPages && !IsLoading && !IsPrinting;
    }
    
    /// <summary>
    /// 当CurrentPage、IsLoading或IsPrinting变化时，更新命令的CanExecute状态
    /// </summary>
    partial void OnCurrentPageChanged(int value)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnIsLoadingChanged(bool value)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnIsPrintingChanged(bool value)
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        PrintCurrentPageCommand.NotifyCanExecuteChanged();
    }
    
    /// <summary>
    /// 打印当前页命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPrintCurrentPage))]
    private async Task PrintCurrentPageAsync()
    {
        if (IsPrinting) return;
        
        try
        {
            IsPrinting = true;
            
            var jobs = new List<PrintJob>();
            var config = await _databaseService.GetConfigAsync();
            
            // 1. 打印当前主单页面
            jobs.Add(new PrintJob
            {
                Options = new PrintOptions
                {
                    FilePath = _pdfFilePath,
                    PrinterName = config.MainOrderPrinter.PrinterName,
                    PaperWidthMm = config.MainOrderPrinter.PaperWidthMm,
                    PaperHeightMm = config.MainOrderPrinter.PaperHeightMm,
                    Copies = 1,
                    PageRange = $"{CurrentPage}" // 只打印当前页
                },
                Description = $"主单第{CurrentPage}页"
            });
            
            // 2. 打印环保码（如果设置了数量）
            if (SelectedEcoCode != null && EcoCodeQuantity > 0)
            {
                var ecoCodePath = System.IO.Path.Combine(
                    _configService.GetAppDataPath(),
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
                        Copies = EcoCodeQuantity
                    },
                    Description = $"环保码: {SelectedEcoCode.Name}"
                });
            }
            else if (SelectedEcoCode == null && EcoCodeQuantity > 0)
            {
                await ShowErrorAsync("请先选择环保码");
                return;
            }
            
            // 3. 打印用户上传的条码分组（如果选择了分组，必须打印）
            if (SelectedBarcodeGroup != null)
            {
                System.Diagnostics.Debug.WriteLine($"[打印] 检查用户条码分组:");
                System.Diagnostics.Debug.WriteLine($"[打印]   SelectedBarcodeGroup: 第{SelectedBarcodeGroup.StartPage}-{SelectedBarcodeGroup.EndPage}页");
                System.Diagnostics.Debug.WriteLine($"[打印]   _barcodePdfFilePath: {(_barcodePdfFilePath ?? "null")}");
                
                if (string.IsNullOrEmpty(_barcodePdfFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[打印] 错误：条码PDF文件路径为空！");
                    await ShowErrorAsync("条码PDF文件路径为空，无法打印条码");
                    return;
                }
                
                if (!System.IO.File.Exists(_barcodePdfFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[打印] 错误：条码PDF文件不存在: {_barcodePdfFilePath}");
                    await ShowErrorAsync($"条码PDF文件不存在: {_barcodePdfFilePath}");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[打印] 准备打印用户上传的条码分组");
                System.Diagnostics.Debug.WriteLine($"[打印] 条码PDF路径: {_barcodePdfFilePath}");
                System.Diagnostics.Debug.WriteLine($"[打印] 分组页码范围: {SelectedBarcodeGroup.StartPage}-{SelectedBarcodeGroup.EndPage}");
                System.Diagnostics.Debug.WriteLine($"[打印] 条码数量: {SelectedBarcodeGroup.BarcodeCount}");
                
                // 构建页码范围字符串
                var pageRange = $"{SelectedBarcodeGroup.StartPage}-{SelectedBarcodeGroup.EndPage}";
                
                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = _barcodePdfFilePath!,
                        PrinterName = config.BarcodePrinter.PrinterName,
                        PaperWidthMm = config.BarcodePrinter.PaperWidthMm,
                        PaperHeightMm = config.BarcodePrinter.PaperHeightMm,
                        Copies = 1,
                        PageRange = pageRange
                    },
                    Description = $"用户条码分组: {SelectedBarcodeGroup.BarcodeCount}个"
                });
                
                System.Diagnostics.Debug.WriteLine($"[打印] 已添加用户条码分组打印任务");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[打印] 未选择用户条码分组，跳过");
            }
            
            // 4. 打印内置英代欧代条码（仅当勾选复选框且数量>0时）
            System.Diagnostics.Debug.WriteLine($"[打印] 检查内置英代欧代条码打印条件:");
            System.Diagnostics.Debug.WriteLine($"[打印]   UkEuBarcodeEnabled: {UkEuBarcodeEnabled}");
            System.Diagnostics.Debug.WriteLine($"[打印]   UkEuBarcodeQuantity: {UkEuBarcodeQuantity}");
            
            if (UkEuBarcodeEnabled && UkEuBarcodeQuantity > 0)
            {
                var ukEuBarcodePath = _printService.GetBuiltinResourcePath("uk_eu_barcode.pdf");
                
                if (System.IO.File.Exists(ukEuBarcodePath))
                {
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
                    
                    System.Diagnostics.Debug.WriteLine($"[打印] 已添加内置英代欧代条码打印任务，数量: {UkEuBarcodeQuantity}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[打印] 警告：内置英代欧代条码文件不存在: {ukEuBarcodePath}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[打印] 未启用内置英代欧代条码打印");
            }
            
            // 执行批量打印
            System.Diagnostics.Debug.WriteLine($"[打印] 共有 {jobs.Count} 个打印任务");
            for (int i = 0; i < jobs.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[打印] 任务 {i + 1}: {jobs[i].Description}");
            }
            
            var result = await _printService.PrintBatchAsync(jobs);
            
            // 打印成功后标记分组为已打印
            if (result.FailedCount == 0 && SelectedBarcodeGroup != null)
            {
                _barcodeGroupService.MarkAsPrinted(SelectedBarcodeGroup.Id);
            }
            
            // 显示结果
            if (result.FailedCount == 0)
            {
                await ShowInfoAsync($"打印完成！成功打印 {result.SuccessCount} 个任务");
            }
            else
            {
                var errorMsg = $"打印完成：成功 {result.SuccessCount} 个，失败 {result.FailedCount} 个\n\n失败任务：\n";
                
                // 显示前5个失败任务
                for (int i = 0; i < Math.Min(5, result.FailedJobs.Count); i++)
                {
                    var (job, error) = result.FailedJobs[i];
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
        }
    }
    
    /// <summary>
    /// 是否可以打印当前页
    /// </summary>
    private bool CanPrintCurrentPage()
    {
        return !IsLoading && !IsPrinting;
    }
    
    /// <summary>
    /// 选择条码分组命令
    /// </summary>
    [RelayCommand]
    private async Task SelectBarcodeGroupAsync()
    {
        if (_barcodeGroups.Count == 0)
        {
            await ShowErrorAsync("未找到条码分组，请确保条码PDF文件存在且包含分隔符");
            return;
        }
        
        try
        {
            var dialog = new Views.LabelSelectionDialog();
            var viewModel = new LabelSelectionViewModel(_barcodeGroups);
            dialog.DataContext = viewModel;
            
            if (OwnerWindow != null)
            {
                await dialog.ShowDialog(OwnerWindow);
                
                if (viewModel.Result != null)
                {
                    SelectedBarcodeGroup = viewModel.Result;
                    
                    // 更新英代欧代条码数量为选中分组的条码数量
                    UkEuBarcodeQuantity = SelectedBarcodeGroup.BarcodeCount;
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"打开标签选择对话框失败: {ex.Message}");
        }
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
