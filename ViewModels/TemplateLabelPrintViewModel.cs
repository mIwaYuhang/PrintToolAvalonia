using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

public partial class TemplateLabelPrintViewModel : ViewModelBase
{
    private const double MainOrderPreviewViewportWidth = 388d;
    private const double MainOrderPreviewViewportHeight = 300d;
    private const double MainOrderPreviewZoomStep = 1.25d;
    private const double MainOrderPreviewMaxZoom = 4d;
    private readonly IFileService _fileService;
    private readonly IPdfRenderService _pdfRenderService;
    private readonly ILabelTemplateService _labelTemplateService;
    private readonly IImageMatchService _imageMatchService;
    private readonly IBarcodeGroupService _barcodeGroupService;
    private readonly IPrintService _printService;
    private readonly IDatabaseService _databaseService;

    private readonly List<BarcodeGroup> _barcodeGroups = new();
    private string _mainOrderPdfPath = string.Empty;
    private double _mainOrderPreviewBaseScale = 1d;
    private Platform _currentPlatform = Platform.TEMU;

    /// <summary>
    /// 当前是否需要显示商品名称（希音与冷希音特供款均需要按条码选择商品名称）
    /// </summary>
    public bool IsSheinPlatform => _currentPlatform == Platform.SHEIN || _currentPlatform == Platform.SHEIN_SPECIAL;

    /// <summary>
    /// 当前是否为冷希音特供款（不合成条码，使用 60x80 布局）
    /// </summary>
    public bool IsSheinSpecialPlatform => _currentPlatform == Platform.SHEIN_SPECIAL;

    /// <summary>
    /// 标签尺寸（毫米），冷希音特供款为 60x80，其余为 100x100
    /// </summary>
    public int LabelWidthMm => _currentPlatform == Platform.SHEIN_SPECIAL ? 60 : 100;

    public int LabelHeightMm => _currentPlatform == Platform.SHEIN_SPECIAL ? 80 : 100;

    /// <summary>
    /// 标签尺寸文本（如 "60 x 80 mm"）
    /// </summary>
    public string LabelSizeText => $"{LabelWidthMm} x {LabelHeightMm} mm";

    /// <summary>
    /// 对话框标题文本
    /// </summary>
    public string HeaderTitleText => $"{LabelWidthMm}mm x {LabelHeightMm}mm 模板标签打印";

    /// <summary>
    /// 对话框说明文本
    /// </summary>
    public string HeaderDescriptionText => IsSheinSpecialPlatform
        ? $"冷希音特供款：不合成条码，直接打印 {LabelWidthMm}x{LabelHeightMm} 环保标签。打印时仍可选择条码分组与商品名称，但条码不会合并进标签。"
        : $"模板由 JSON 配置驱动，条码会合并进模板 PDF。打印时会先输出当前主单页，再输出后续 {LabelWidthMm}x{LabelHeightMm} 模板标签。";

    /// <summary>
    /// 队列底部说明文本
    /// </summary>
    public string QueueHintText => IsSheinSpecialPlatform
        ? $"说明：冷希音特供款不合成条码，每个条码分组按组内数量生成 {LabelWidthMm}x{LabelHeightMm} 环保标签；点击\u201c生成并打印\u201d时会先打印左侧当前主单页，点击\u201c补打模板标签\u201d只补打模板标签。"
        : $"说明：每个条码分组只取首条条码，按组内数量生成 {LabelWidthMm}x{LabelHeightMm} 模板标签；点击\u201c生成并打印\u201d时会先打印左侧当前主单页，点击\u201c补打模板标签\u201d只补打模板标签。";

    public Window? OwnerWindow { get; set; }

    public ObservableCollection<LabelTemplateConfig> TemplateConfigs { get; } = new();

    public ObservableCollection<TemplateLabelQueueItem> QueueItems { get; } = new();

    /// <summary>
    /// 商品名称列表（供希音平台选择）
    /// </summary>
    public ObservableCollection<ProductNameItem> ProductNameItems { get; } = new();

    [ObservableProperty]
    private LabelTemplateConfig? _selectedTemplateConfig;

    [ObservableProperty]
    private bool _includeImporterInfo = true;

    [ObservableProperty]
    private string _barcodePdfPath = string.Empty;

    [ObservableProperty]
    private bool _isScanningBarcode;

    [ObservableProperty]
    private bool _isLoadingMainOrderPage;

    [ObservableProperty]
    private bool _isPrinting;

    [ObservableProperty]
    private string _statusMessage = "请先选择模板和条码 PDF";

    [ObservableProperty]
    private int _currentMainOrderPage = 1;

    [ObservableProperty]
    private int _mainOrderTotalPages;

    [ObservableProperty]
    private Bitmap? _currentMainOrderImage;

    [ObservableProperty]
    private double _mainOrderPreviewZoom = 1d;

    public int TotalLabelCount => QueueItems.Where(item => !item.IsBasePrintCompleted).Sum(item => item.LabelCount);

    public int TotalReprintCount => QueueItems.Sum(item => Math.Max(0, item.ReprintCount));

    public int PendingBasePrintGroupCount => QueueItems.Count(item => !item.IsBasePrintCompleted);

    public double MainOrderPreviewDisplayWidth => CurrentMainOrderImage == null
        ? 0
        : CurrentMainOrderImage.PixelSize.Width * _mainOrderPreviewBaseScale * MainOrderPreviewZoom;

    public double MainOrderPreviewDisplayHeight => CurrentMainOrderImage == null
        ? 0
        : CurrentMainOrderImage.PixelSize.Height * _mainOrderPreviewBaseScale * MainOrderPreviewZoom;

    public string MainOrderPreviewZoomText => $"{Math.Round(MainOrderPreviewZoom * 100):0}%";

    public string MainOrderFileName => string.IsNullOrWhiteSpace(_mainOrderPdfPath)
        ? "未加载主单"
        : Path.GetFileName(_mainOrderPdfPath);

    public string CurrentBarcodeFileName => string.IsNullOrWhiteSpace(BarcodePdfPath)
        ? "未选择"
        : Path.GetFileName(BarcodePdfPath);

    public string QueueSummary => QueueItems.Count == 0
        ? "待打印队列为空"
        : PendingBasePrintGroupCount == 0
            ? TotalReprintCount > 0
                ? $"当前队列已完成首打，待补打 {TotalReprintCount} 张"
                : "当前队列已完成首打，可按需填写补打数量"
        : TotalReprintCount > 0
            ? $"待首打 {PendingBasePrintGroupCount} 个分组，共 {TotalLabelCount} 张标签；待补打 {TotalReprintCount} 张"
            : $"待首打 {PendingBasePrintGroupCount} 个分组，共 {TotalLabelCount} 张标签";

    public TemplateLabelPrintViewModel(
        IFileService fileService,
        IPdfRenderService pdfRenderService,
        ILabelTemplateService labelTemplateService,
        IImageMatchService imageMatchService,
        IBarcodeGroupService barcodeGroupService,
        IPrintService printService,
        IDatabaseService databaseService)
    {
        _fileService = fileService;
        _pdfRenderService = pdfRenderService;
        _labelTemplateService = labelTemplateService;
        _imageMatchService = imageMatchService;
        _barcodeGroupService = barcodeGroupService;
        _printService = printService;
        _databaseService = databaseService;

        QueueItems.CollectionChanged += OnQueueItemsChanged;
    }

    partial void OnCurrentMainOrderImageChanged(Bitmap? value)
    {
        RecalculateMainOrderPreviewBaseScale();
        MainOrderPreviewZoom = 1d;
        NotifyMainOrderPreviewChanged();
        ZoomInMainOrderPreviewCommand.NotifyCanExecuteChanged();
        ZoomOutMainOrderPreviewCommand.NotifyCanExecuteChanged();
        ResetMainOrderPreviewZoomCommand.NotifyCanExecuteChanged();
    }

    partial void OnMainOrderPreviewZoomChanged(double value)
    {
        NotifyMainOrderPreviewChanged();
        ZoomInMainOrderPreviewCommand.NotifyCanExecuteChanged();
        ZoomOutMainOrderPreviewCommand.NotifyCanExecuteChanged();
        ResetMainOrderPreviewZoomCommand.NotifyCanExecuteChanged();
    }

    public async Task InitializeAsync(string mainOrderPdfPath, string? initialBarcodePdfPath = null, int initialPage = 1, Platform platform = Platform.TEMU)
    {
        _mainOrderPdfPath = mainOrderPdfPath;
        _currentPlatform = platform;
        OnPropertyChanged(nameof(IsSheinPlatform));
        OnPropertyChanged(nameof(IsSheinSpecialPlatform));
        OnPropertyChanged(nameof(LabelWidthMm));
        OnPropertyChanged(nameof(LabelHeightMm));
        OnPropertyChanged(nameof(LabelSizeText));
        OnPropertyChanged(nameof(HeaderTitleText));
        OnPropertyChanged(nameof(HeaderDescriptionText));
        OnPropertyChanged(nameof(QueueHintText));
        await LoadTemplatesAsync();
        await LoadProductNamesAsync();
        await InitializeMainOrderAsync(initialPage);

        if (!string.IsNullOrWhiteSpace(initialBarcodePdfPath) && File.Exists(initialBarcodePdfPath))
        {
            await LoadBarcodeGroupsAsync(initialBarcodePdfPath);
        }
    }

    [RelayCommand]
    private async Task ChooseBarcodePdfAsync()
    {
        var files = await _fileService.OpenFileDialogAsync("PDF Files|*.pdf", OwnerWindow);
        if (files.Length == 0)
        {
            return;
        }

        await LoadBarcodeGroupsAsync(files[0]);
    }

    [RelayCommand]
    private async Task AddBarcodeGroupAsync()
    {
        if (_barcodeGroups.Count == 0)
        {
            await ShowErrorAsync("请先选择并加载条码 PDF");
            return;
        }

        if (SelectedTemplateConfig == null)
        {
            await ShowErrorAsync("请先选择模板");
            return;
        }

        try
        {
            var dialog = new Views.LabelSelectionDialog();
            var viewModel = new LabelSelectionViewModel(_barcodeGroups);
            dialog.DataContext = viewModel;

            if (OwnerWindow == null)
            {
                await ShowErrorAsync("当前窗口未初始化");
                return;
            }

            await dialog.ShowDialog(OwnerWindow);
            if (viewModel.Result == null)
            {
                return;
            }

            QueueItems.Add(new TemplateLabelQueueItem
            {
                BarcodePdfPath = BarcodePdfPath,
                BarcodeGroup = viewModel.Result,
                TemplateName = SelectedTemplateConfig.Name,
                IsBasePrintCompleted = viewModel.Result.IsPrinted
            });

            StatusMessage = $"已加入分组：第{viewModel.Result.StartPage}-{viewModel.Result.EndPage}页";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"选择条码分组失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportSinglePdfAsync(TemplateLabelQueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        if (SelectedTemplateConfig == null)
        {
            await ShowErrorAsync("请先选择模板");
            return;
        }

        if (IsSheinPlatform && string.IsNullOrWhiteSpace(item.ProductNameEnglish))
        {
            await ShowErrorAsync($"{item.GroupDisplay} 未设置商品名称（英文打印名）");
            return;
        }

        try
        {
            IsPrinting = true;
            StatusMessage = $"正在生成 {item.GroupDisplay} 的单张 PDF...";

            var generatedPdfPath = await _labelTemplateService.GenerateLabelPdfAsync(
                SelectedTemplateConfig,
                item.BarcodePdfPath,
                item.BarcodeGroup.StartPage,
                IncludeImporterInfo,
                item.ProductNameEnglish);

            var suggestedFileName = BuildExportFileName(SelectedTemplateConfig, item);
            var exportedPath = await _fileService.SavePdfAsync(generatedPdfPath, suggestedFileName, OwnerWindow);
            if (exportedPath == null)
            {
                StatusMessage = "已取消导出";
                return;
            }

            StatusMessage = $"已导出 1 张标签：{Path.GetFileName(exportedPath)}";
            await ShowInfoAsync($"单张标签 PDF 已导出：\n{exportedPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = "导出失败";
            await ShowErrorAsync($"导出单张标签 PDF 失败: {ex.Message}");
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private static string BuildExportFileName(LabelTemplateConfig template, TemplateLabelQueueItem item)
    {
        var sourceName = Path.GetFileNameWithoutExtension(item.BarcodePdfPath);
        var fileName = $"{template.Name}_{sourceName}_第{item.BarcodeGroup.StartPage}页.pdf";
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }

    [RelayCommand]
    private void RemoveQueueItem(TemplateLabelQueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        QueueItems.Remove(item);
    }

    [RelayCommand]
    private void ClearQueue()
    {
        QueueItems.Clear();
        StatusMessage = "待打印队列已清空";
    }

    [RelayCommand]
    private async Task CreateTemplateAsync()
    {
        await OpenTemplateEditorAsync(null);
    }

    [RelayCommand]
    private async Task EditSelectedTemplateAsync()
    {
        if (SelectedTemplateConfig == null)
        {
            await ShowErrorAsync("请先选择模板");
            return;
        }

        await OpenTemplateEditorAsync(SelectedTemplateConfig);
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousMainOrderPage))]
    private async Task PreviousMainOrderPageAsync()
    {
        if (CurrentMainOrderPage <= 1)
        {
            return;
        }

        CurrentMainOrderPage--;
        await LoadMainOrderPageAsync(CurrentMainOrderPage);
    }

    [RelayCommand(CanExecute = nameof(CanGoNextMainOrderPage))]
    private async Task NextMainOrderPageAsync()
    {
        if (CurrentMainOrderPage >= MainOrderTotalPages)
        {
            return;
        }

        CurrentMainOrderPage++;
        await LoadMainOrderPageAsync(CurrentMainOrderPage);
    }

    [RelayCommand(CanExecute = nameof(CanZoomOutMainOrderPreview))]
    private void ZoomOutMainOrderPreview()
    {
        MainOrderPreviewZoom = Math.Max(1d, MainOrderPreviewZoom / MainOrderPreviewZoomStep);
    }

    [RelayCommand(CanExecute = nameof(CanResetMainOrderPreviewZoom))]
    private void ResetMainOrderPreviewZoom()
    {
        MainOrderPreviewZoom = 1d;
    }

    [RelayCommand(CanExecute = nameof(CanZoomInMainOrderPreview))]
    private void ZoomInMainOrderPreview()
    {
        MainOrderPreviewZoom = Math.Min(MainOrderPreviewMaxZoom, MainOrderPreviewZoom * MainOrderPreviewZoomStep);
    }

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private async Task PrintAsync()
    {
        if (SelectedTemplateConfig == null)
        {
            await ShowErrorAsync("请先选择模板");
            return;
        }

        var pendingItems = QueueItems.Where(item => !item.IsBasePrintCompleted).ToList();
        if (pendingItems.Count == 0)
        {
            await ShowErrorAsync("当前队列没有待首打分组，请设置补打数量后使用补打模板标签");
            return;
        }

        // 希音 / 冷希音特供款：校验每个待打印分组必须填写商品名称（英文）
        if (IsSheinPlatform)
        {
            var missingProductName = pendingItems.Where(item => string.IsNullOrWhiteSpace(item.ProductNameEnglish)).ToList();
            if (missingProductName.Count > 0)
            {
                var groupInfo = string.Join("、", missingProductName.Select(item => item.GroupDisplay));
                await ShowErrorAsync($"以下分组未设置商品名称（英文打印名）：{groupInfo}\n\n该平台要求每个条码分组必须设置商品名称。");
                return;
            }
        }

        try
        {
            IsPrinting = true;
            StatusMessage = "正在生成模板打印任务...";
            var pendingLabelCount = pendingItems.Sum(item => item.LabelCount);

            var config = await _databaseService.GetConfigAsync();
            if (string.IsNullOrWhiteSpace(config.MainOrderPrinter.PrinterName))
            {
                await ShowErrorAsync("请先在设置中配置主单打印机");
                return;
            }

            if (config.MainOrderPrinter.PaperWidthMm <= 0 || config.MainOrderPrinter.PaperHeightMm <= 0)
            {
                await ShowErrorAsync("请先在设置中配置主单打印机纸张尺寸");
                return;
            }

            // 冷希音特供款：模板标签使用环保码打印机，需要校验其配置
            var templateLabelPrinter = GetTemplateLabelPrinter(config);
            if (IsSheinSpecialPlatform && string.IsNullOrWhiteSpace(templateLabelPrinter.PrinterName))
            {
                await ShowErrorAsync("请先在设置中配置环保码打印机");
                return;
            }

            var jobs = new List<PrintJob>();

            // 希音 / 冷希音特供款：只有一个面单，仅在第一次首打时打印主单
            // Temu 平台：每次打印都先输出当前主单页
            bool shouldPrintMainOrder;
            if (IsSheinPlatform)
            {
                // 希音：只有当队列中没有任何已完成首打的分组时，才打印主单（即第一次打印）
                shouldPrintMainOrder = !QueueItems.Any(item => item.IsBasePrintCompleted);
            }
            else
            {
                shouldPrintMainOrder = true;
            }

            if (shouldPrintMainOrder)
            {
                jobs.Add(new PrintJob
                {
                    Options = new PrintOptions
                    {
                        FilePath = _mainOrderPdfPath,
                        PrinterName = config.MainOrderPrinter.PrinterName,
                        PaperWidthMm = config.MainOrderPrinter.PaperWidthMm,
                        PaperHeightMm = config.MainOrderPrinter.PaperHeightMm,
                        Copies = 1,
                        PageRange = $"{CurrentMainOrderPage}"
                    },
                    Description = $"主单第{CurrentMainOrderPage}页"
                });
            }

            jobs.AddRange(await CreateTemplateLabelJobsAsync(
                SelectedTemplateConfig,
                pendingItems,
                item => item.LabelCount,
                IncludeImporterInfo,
                templateLabelPrinter.PrinterName,
                "模板标签"));

            StatusMessage = shouldPrintMainOrder
                ? "模板打印任务已生成，正在先打印主单页，再打印模板标签..."
                : "模板打印任务已生成，正在打印模板标签（主单已在首次打印时输出）...";

            var result = await _printService.PrintBatchAsync(jobs);

            if (result.FailedCount == 0)
            {
                foreach (var item in pendingItems)
                {
                    _barcodeGroupService.MarkAsPrinted(item.BarcodeGroup.Id);
                    item.IsBasePrintCompleted = true;
                }

                StatusMessage = $"首打完成，共输出 {pendingLabelCount} 张标签";
                await ShowInfoAsync($"{StatusMessage}。如需补打，请在队列中填写补打数量后点击“补打模板标签”。");
            }
            else
            {
                var message = string.Join(
                    Environment.NewLine,
                    result.FailedJobs.Select(job => $"- {job.Job.Description}: {job.Error}").Take(5));
                await ShowErrorAsync($"打印失败：{Environment.NewLine}{message}");
                StatusMessage = "打印失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "打印失败";
            await ShowErrorAsync($"模板标签打印失败: {ex.Message}");
        }
        finally
        {
            IsPrinting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanReprint))]
    private async Task ReprintAsync()
    {
        if (SelectedTemplateConfig == null)
        {
            await ShowErrorAsync("请先选择模板");
            return;
        }

        var reprintItems = QueueItems.Where(item => item.ReprintCount > 0).ToList();
        if (reprintItems.Count == 0)
        {
            await ShowErrorAsync("请先设置补打数量");
            return;
        }

        // 希音 / 冷希音特供款：补打也需要校验商品名称
        if (IsSheinPlatform)
        {
            var missingProductName = reprintItems.Where(item => string.IsNullOrWhiteSpace(item.ProductNameEnglish)).ToList();
            if (missingProductName.Count > 0)
            {
                var groupInfo = string.Join("、", missingProductName.Select(item => item.GroupDisplay));
                await ShowErrorAsync($"以下分组未设置商品名称（英文打印名）：{groupInfo}\n\n该平台要求每个条码分组必须设置商品名称。");
                return;
            }
        }

        try
        {
            IsPrinting = true;
            StatusMessage = "正在生成补打打印任务...";
            var reprintLabelCount = reprintItems.Sum(item => item.ReprintCount);

            var config = await _databaseService.GetConfigAsync();

            // 冷希音特供款：模板标签使用环保码打印机；其余使用主单打印机
            var templateLabelPrinter = GetTemplateLabelPrinter(config);
            if (string.IsNullOrWhiteSpace(templateLabelPrinter.PrinterName))
            {
                await ShowErrorAsync($"请先在设置中配置{GetTemplateLabelPrinterDisplayName()}");
                return;
            }

            var jobs = await CreateTemplateLabelJobsAsync(
                SelectedTemplateConfig,
                reprintItems,
                item => item.ReprintCount,
                IncludeImporterInfo,
                templateLabelPrinter.PrinterName,
                "模板标签补打");

            StatusMessage = "补打打印任务已生成，正在打印模板标签...";

            var result = await _printService.PrintBatchAsync(jobs);

            if (result.FailedCount == 0)
            {
                foreach (var item in reprintItems)
                {
                    item.ReprintCount = 0;
                }

                StatusMessage = $"补打完成，共输出 {reprintLabelCount} 张标签";
                await ShowInfoAsync(StatusMessage);
            }
            else
            {
                var message = string.Join(
                    Environment.NewLine,
                    result.FailedJobs.Select(job => $"- {job.Job.Description}: {job.Error}").Take(5));
                await ShowErrorAsync($"补打失败：{Environment.NewLine}{message}");
                StatusMessage = "补打失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "补打失败";
            await ShowErrorAsync($"模板标签补打失败: {ex.Message}");
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private bool CanPrint()
    {
        return !IsPrinting &&
               !IsLoadingMainOrderPage &&
               SelectedTemplateConfig != null &&
               QueueItems.Any(item => !item.IsBasePrintCompleted) &&
               !string.IsNullOrWhiteSpace(_mainOrderPdfPath);
    }

    private bool CanReprint()
    {
        return !IsPrinting &&
               SelectedTemplateConfig != null &&
               QueueItems.Any(item => item.ReprintCount > 0);
    }

    private bool CanGoPreviousMainOrderPage()
    {
        return CurrentMainOrderPage > 1 && !IsLoadingMainOrderPage && !IsPrinting;
    }

    private bool CanGoNextMainOrderPage()
    {
        return CurrentMainOrderPage < MainOrderTotalPages && !IsLoadingMainOrderPage && !IsPrinting;
    }

    partial void OnSelectedTemplateConfigChanged(LabelTemplateConfig? value)
    {
        foreach (var item in QueueItems)
        {
            item.TemplateName = value?.Name ?? string.Empty;
        }

        OnPropertyChanged(nameof(QueueSummary));
        PrintCommand.NotifyCanExecuteChanged();
        ReprintCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPrintingChanged(bool value)
    {
        PreviousMainOrderPageCommand.NotifyCanExecuteChanged();
        NextMainOrderPageCommand.NotifyCanExecuteChanged();
        PrintCommand.NotifyCanExecuteChanged();
        ReprintCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingMainOrderPageChanged(bool value)
    {
        PreviousMainOrderPageCommand.NotifyCanExecuteChanged();
        NextMainOrderPageCommand.NotifyCanExecuteChanged();
        PrintCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentMainOrderPageChanged(int value)
    {
        PreviousMainOrderPageCommand.NotifyCanExecuteChanged();
        NextMainOrderPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnBarcodePdfPathChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentBarcodeFileName));
    }

    private void OnQueueItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TemplateLabelQueueItem>())
            {
                item.PropertyChanged -= OnQueueItemPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TemplateLabelQueueItem>())
            {
                item.PropertyChanged += OnQueueItemPropertyChanged;
            }
        }

        NotifyQueueStateChanged();
    }

    private void OnQueueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TemplateLabelQueueItem.ReprintCount) ||
            e.PropertyName == nameof(TemplateLabelQueueItem.IsBasePrintCompleted))
        {
            NotifyQueueStateChanged();
        }
    }

    private void NotifyQueueStateChanged()
    {
        OnPropertyChanged(nameof(TotalLabelCount));
        OnPropertyChanged(nameof(TotalReprintCount));
        OnPropertyChanged(nameof(PendingBasePrintGroupCount));
        OnPropertyChanged(nameof(QueueSummary));
        PrintCommand.NotifyCanExecuteChanged();
        ReprintCommand.NotifyCanExecuteChanged();
    }

    private async Task<List<PrintJob>> CreateTemplateLabelJobsAsync(
        LabelTemplateConfig template,
        IEnumerable<TemplateLabelQueueItem> queueItems,
        Func<TemplateLabelQueueItem, int> copySelector,
        bool includeImporterInfo,
        string printerName,
        string descriptionPrefix)
    {
        var jobs = new List<PrintJob>();

        foreach (var item in queueItems)
        {
            var copies = Math.Max(0, copySelector(item));
            if (copies == 0)
            {
                continue;
            }

            var pdfPath = await _labelTemplateService.GenerateLabelPdfAsync(
                template,
                item.BarcodePdfPath,
                item.BarcodeGroup.StartPage,
                includeImporterInfo,
                item.ProductNameEnglish);

            jobs.Add(new PrintJob
            {
                Options = new PrintOptions
                {
                    FilePath = pdfPath,
                    PrinterName = printerName,
                    PaperWidthMm = LabelWidthMm,
                    PaperHeightMm = LabelHeightMm,
                    Copies = copies
                },
                Description = $"{descriptionPrefix}: {template.Name} 第{item.BarcodeGroup.StartPage}-{item.BarcodeGroup.EndPage}页 ({copies}份)"
            });
        }

        return jobs;
    }

    private async Task LoadTemplatesAsync()
    {
        await LoadTemplatesAsync(null);
    }

    private async Task LoadProductNamesAsync()
    {
        try
        {
            var items = await _databaseService.GetAllProductNamesAsync();
            ProductNameItems.Clear();
            foreach (var item in items)
            {
                ProductNameItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载商品名称失败: {ex.Message}");
        }
    }

    private async Task LoadTemplatesAsync(string? selectedTemplateId)
    {
        try
        {
            var templates = await _labelTemplateService.GetTemplatesAsync();
            TemplateConfigs.Clear();

            // 按平台过滤模板：希音只显示 shein 模板，冷希音特供款显示 shein_special 模板，Temu 只显示 temu 模板
            var expectedVariant = _currentPlatform switch
            {
                Platform.SHEIN => "shein",
                Platform.SHEIN_SPECIAL => "shein_special",
                _ => "temu"
            };
            foreach (var template in templates)
            {
                var variant = string.IsNullOrWhiteSpace(template.LayoutVariant) ? "temu" : template.LayoutVariant.ToLowerInvariant();
                if (string.Equals(variant, expectedVariant, StringComparison.OrdinalIgnoreCase))
                {
                    TemplateConfigs.Add(template);
                }
            }

            SelectedTemplateConfig = !string.IsNullOrWhiteSpace(selectedTemplateId)
                ? TemplateConfigs.FirstOrDefault(template => string.Equals(template.Id, selectedTemplateId, StringComparison.OrdinalIgnoreCase))
                    ?? TemplateConfigs.FirstOrDefault()
                : TemplateConfigs.FirstOrDefault();
            StatusMessage = TemplateConfigs.Count == 0
                ? $"未找到{GetPlatformDisplayName()}模板，请先新建对应模板配置"
                : "请选择条码分组加入待打印队列";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"加载模板失败: {ex.Message}");
        }
    }

    private string GetPlatformDisplayName()
    {
        return _currentPlatform switch
        {
            Platform.SHEIN => "希音",
            Platform.SHEIN_SPECIAL => "冷希音特供款",
            _ => "Temu"
        };
    }

    /// <summary>
    /// 模板标签使用的打印机：冷希音特供款使用环保码打印机，其余使用主单打印机
    /// </summary>
    private PrinterConfig GetTemplateLabelPrinter(AppConfig config)
    {
        return IsSheinSpecialPlatform ? config.EcoCodePrinter : config.MainOrderPrinter;
    }

    /// <summary>
    /// 模板标签打印机的名称（用于提示文案）
    /// </summary>
    private string GetTemplateLabelPrinterDisplayName()
    {
        return IsSheinSpecialPlatform ? "环保码打印机" : "主单打印机";
    }

    private async Task OpenTemplateEditorAsync(LabelTemplateConfig? template)
    {
        try
        {
            var dialog = App.Services?.GetRequiredService<Views.LabelTemplateEditorDialog>();
            var viewModel = App.Services?.GetRequiredService<LabelTemplateEditorViewModel>();
            if (dialog == null || viewModel == null)
            {
                await ShowErrorAsync("模板编辑器初始化失败");
                return;
            }

            var previewBarcodePageNumber = GetPreviewBarcodePageNumber();
            var previewBarcodePdfPath = GetPreviewBarcodePdfPath();

            var newTemplateVariant = _currentPlatform switch
            {
                Platform.SHEIN => "shein",
                Platform.SHEIN_SPECIAL => "shein_special",
                _ => "temu"
            };

            await viewModel.InitializeAsync(template, previewBarcodePdfPath, previewBarcodePageNumber, IncludeImporterInfo, newTemplateVariant);
            dialog.DataContext = viewModel;
            await dialog.ShowDialog(OwnerWindow ?? GetDialogOwner());

            if (viewModel.SavedTemplate != null)
            {
                await LoadTemplatesAsync(viewModel.SavedTemplate.Id);
                StatusMessage = $"模板已保存：{viewModel.SavedTemplate.Name}";
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"打开模板编辑器失败: {ex.Message}");
        }
    }

    private string? GetPreviewBarcodePdfPath()
    {
        if (QueueItems.Count > 0)
        {
            return QueueItems[0].BarcodePdfPath;
        }

        return string.IsNullOrWhiteSpace(BarcodePdfPath) ? null : BarcodePdfPath;
    }

    private int? GetPreviewBarcodePageNumber()
    {
        if (QueueItems.Count > 0)
        {
            return QueueItems[0].BarcodeGroup.StartPage;
        }

        if (_barcodeGroups.Count > 0)
        {
            return _barcodeGroups[0].StartPage;
        }

        return null;
    }

    private Window? GetDialogOwner()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private async Task InitializeMainOrderAsync(int initialPage)
    {
        if (string.IsNullOrWhiteSpace(_mainOrderPdfPath) || !File.Exists(_mainOrderPdfPath))
        {
            throw new FileNotFoundException($"主单文件不存在: {_mainOrderPdfPath}");
        }

        MainOrderTotalPages = await _pdfRenderService.GetPageCountAsync(_mainOrderPdfPath);
        CurrentMainOrderPage = Math.Clamp(initialPage, 1, Math.Max(1, MainOrderTotalPages));
        OnPropertyChanged(nameof(MainOrderFileName));
        await LoadMainOrderPageAsync(CurrentMainOrderPage);
    }

    private async Task LoadMainOrderPageAsync(int pageNumber)
    {
        try
        {
            IsLoadingMainOrderPage = true;
            CurrentMainOrderImage = await _pdfRenderService.RenderPageAsync(_mainOrderPdfPath, pageNumber);
            StatusMessage = $"当前主单页：第 {pageNumber} / {MainOrderTotalPages} 页，打印时会先打印该页，再打印模板标签";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"加载主单页面失败: {ex.Message}");
        }
        finally
        {
            IsLoadingMainOrderPage = false;
        }
    }

    private async Task LoadBarcodeGroupsAsync(string pdfPath)
    {
        try
        {
            IsScanningBarcode = true;
            StatusMessage = "正在识别条码分组...";

            // 根据平台选择分隔符模板（希音与冷希音特供款使用希音分隔符）
            var separatorFileName = IsSheinPlatform
                ? "shien_separator_template.png"
                : "separator_template.png";

            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", separatorFileName);
            if (!File.Exists(templatePath))
            {
                await ShowErrorAsync($"分隔符模板不存在: {templatePath}");
                return;
            }

            _imageMatchService.LoadTemplate(templatePath);
            var separatorPages = await _imageMatchService.ScanSeparatorsAsync(pdfPath);
            var groups = await _barcodeGroupService.CreateGroupsAsync(pdfPath, separatorPages);

            _barcodeGroups.Clear();
            _barcodeGroups.AddRange(groups);
            BarcodePdfPath = pdfPath;
            StatusMessage = _barcodeGroups.Count == 0
                ? "未识别到可用条码分组"
                : $"已加载 {Path.GetFileName(pdfPath)}，共识别 {_barcodeGroups.Count} 个条码分组";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"加载条码 PDF 失败: {ex.Message}");
        }
        finally
        {
            IsScanningBarcode = false;
        }
    }

    private bool CanZoomInMainOrderPreview()
    {
        return CurrentMainOrderImage != null && MainOrderPreviewZoom < MainOrderPreviewMaxZoom;
    }

    private bool CanZoomOutMainOrderPreview()
    {
        return CurrentMainOrderImage != null && MainOrderPreviewZoom > 1d;
    }

    private bool CanResetMainOrderPreviewZoom()
    {
        return CurrentMainOrderImage != null && Math.Abs(MainOrderPreviewZoom - 1d) > 0.001d;
    }

    private void RecalculateMainOrderPreviewBaseScale()
    {
        if (CurrentMainOrderImage == null ||
            CurrentMainOrderImage.PixelSize.Width <= 0 ||
            CurrentMainOrderImage.PixelSize.Height <= 0)
        {
            _mainOrderPreviewBaseScale = 1d;
            return;
        }

        var widthScale = MainOrderPreviewViewportWidth / CurrentMainOrderImage.PixelSize.Width;
        var heightScale = MainOrderPreviewViewportHeight / CurrentMainOrderImage.PixelSize.Height;
        _mainOrderPreviewBaseScale = Math.Min(widthScale, heightScale);
    }

    private void NotifyMainOrderPreviewChanged()
    {
        OnPropertyChanged(nameof(MainOrderPreviewDisplayWidth));
        OnPropertyChanged(nameof(MainOrderPreviewDisplayHeight));
        OnPropertyChanged(nameof(MainOrderPreviewZoomText));
    }

    private async Task ShowErrorAsync(string message)
    {
        await Views.MessageDialog.ShowErrorAsync(OwnerWindow, message);
    }

    private async Task ShowInfoAsync(string message)
    {
        await Views.MessageDialog.ShowInfoAsync(OwnerWindow, message);
    }
}
