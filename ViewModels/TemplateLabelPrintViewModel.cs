using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public Window? OwnerWindow { get; set; }

    public ObservableCollection<LabelTemplateConfig> TemplateConfigs { get; } = new();

    public ObservableCollection<TemplateLabelQueueItem> QueueItems { get; } = new();

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

    public int TotalLabelCount => QueueItems.Sum(item => item.LabelCount);

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
        : $"已加入 {QueueItems.Count} 个分组，共 {TotalLabelCount} 张标签";

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

    public async Task InitializeAsync(string mainOrderPdfPath, string? initialBarcodePdfPath = null, int initialPage = 1)
    {
        _mainOrderPdfPath = mainOrderPdfPath;
        await LoadTemplatesAsync();
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
                TemplateName = SelectedTemplateConfig.Name
            });

            StatusMessage = $"已加入分组：第{viewModel.Result.StartPage}-{viewModel.Result.EndPage}页";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"选择条码分组失败: {ex.Message}");
        }
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

        if (QueueItems.Count == 0)
        {
            await ShowErrorAsync("请先添加要打印的条码分组");
            return;
        }

        try
        {
            IsPrinting = true;
            StatusMessage = "正在生成模板 PDF...";

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

            var pdfPath = await _labelTemplateService.GeneratePdfAsync(
                SelectedTemplateConfig,
                QueueItems.ToList(),
                IncludeImporterInfo);

            StatusMessage = "模板 PDF 已生成，正在先打印当前主单页，再打印模板标签...";

            var jobs = new List<PrintJob>
            {
                new()
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
                },
                new()
                {
                    Options = new PrintOptions
                    {
                        FilePath = pdfPath,
                        PrinterName = config.MainOrderPrinter.PrinterName,
                        PaperWidthMm = 100,
                        PaperHeightMm = 100,
                        Copies = 1
                    },
                    Description = $"模板标签: {SelectedTemplateConfig.Name}"
                }
            };

            var result = await _printService.PrintBatchAsync(jobs);

            if (result.FailedCount == 0)
            {
                foreach (var item in QueueItems)
                {
                    _barcodeGroupService.MarkAsPrinted(item.BarcodeGroup.Id);
                }

                StatusMessage = $"打印完成，共输出 {TotalLabelCount} 张标签";
                await ShowInfoAsync(StatusMessage);
                QueueItems.Clear();
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

    private bool CanPrint()
    {
        return !IsPrinting && !IsLoadingMainOrderPage && SelectedTemplateConfig != null && QueueItems.Count > 0 && !string.IsNullOrWhiteSpace(_mainOrderPdfPath);
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
    }

    partial void OnIsPrintingChanged(bool value)
    {
        PreviousMainOrderPageCommand.NotifyCanExecuteChanged();
        NextMainOrderPageCommand.NotifyCanExecuteChanged();
        PrintCommand.NotifyCanExecuteChanged();
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
        OnPropertyChanged(nameof(TotalLabelCount));
        OnPropertyChanged(nameof(QueueSummary));
        PrintCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadTemplatesAsync()
    {
        await LoadTemplatesAsync(null);
    }

    private async Task LoadTemplatesAsync(string? selectedTemplateId)
    {
        try
        {
            var templates = await _labelTemplateService.GetTemplatesAsync();
            TemplateConfigs.Clear();
            foreach (var template in templates)
            {
                TemplateConfigs.Add(template);
            }

            SelectedTemplateConfig = !string.IsNullOrWhiteSpace(selectedTemplateId)
                ? TemplateConfigs.FirstOrDefault(template => string.Equals(template.Id, selectedTemplateId, StringComparison.OrdinalIgnoreCase))
                    ?? TemplateConfigs.FirstOrDefault()
                : TemplateConfigs.FirstOrDefault();
            StatusMessage = TemplateConfigs.Count == 0
                ? "未找到模板，请先新建模板配置"
                : "请选择条码分组加入待打印队列";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"加载模板失败: {ex.Message}");
        }
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

            await viewModel.InitializeAsync(template, previewBarcodePdfPath, previewBarcodePageNumber, IncludeImporterInfo);
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

            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "separator_template.png");
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
