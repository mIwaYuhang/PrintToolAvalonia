using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// OCR区域配置对话框 ViewModel
/// </summary>
public partial class OcrRegionConfigViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IPdfRenderService _pdfRenderService;
    private readonly IDatabaseService _databaseService;
    
    /// <summary>
    /// 父窗口引用
    /// </summary>
    public Avalonia.Controls.Window? OwnerWindow { get; set; }

    // ========== 预览图像 ==========
    
    private Bitmap? _previewImage;
    /// <summary>
    /// PDF预览图像
    /// </summary>
    public Bitmap? PreviewImage
    {
        get => _previewImage;
        set
        {
            SetProperty(ref _previewImage, value);
            OnPropertyChanged(nameof(HasPreviewImage));
        }
    }
    
    /// <summary>
    /// 是否有预览图像
    /// </summary>
    public bool HasPreviewImage => PreviewImage != null;

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

    // ========== 当前选择的区域类型 ==========
    
    /// <summary>
    /// 区域类型列表
    /// </summary>
    public ObservableCollection<string> RegionTypes { get; } = new()
    {
        "快递单号",
        "件数"
    };
    
    private string _currentRegionType = "快递单号";
    /// <summary>
    /// 当前选择的区域类型
    /// </summary>
    public string CurrentRegionType
    {
        get => _currentRegionType;
        set
        {
            SetProperty(ref _currentRegionType, value);
            OnPropertyChanged(nameof(IsTrackingNumberRegionVisible));
            OnPropertyChanged(nameof(IsPackageCountRegionVisible));
            
            // 切换区域类型时触发UI更新
            RegionsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// 快递单号区域是否可见
    /// </summary>
    public bool IsTrackingNumberRegionVisible => CurrentRegionType == "快递单号";
    
    /// <summary>
    /// 件数区域是否可见
    /// </summary>
    public bool IsPackageCountRegionVisible => CurrentRegionType == "件数";

    // ========== 命令 ==========
    
    public ICommand LoadPdfCommand { get; }
    public ICommand ResetCurrentRegionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    // ========== 事件 ==========
    
    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event EventHandler? RequestClose;
    
    /// <summary>
    /// 区域更新事件
    /// </summary>
    public event EventHandler? RegionsUpdated;

    public OcrRegionConfigViewModel(
        IFileService fileService,
        IPdfRenderService pdfRenderService,
        IDatabaseService databaseService)
    {
        _fileService = fileService;
        _pdfRenderService = pdfRenderService;
        _databaseService = databaseService;

        // 初始化命令
        LoadPdfCommand = new AsyncRelayCommand(LoadPdfAsync);
        ResetCurrentRegionCommand = new RelayCommand(ResetCurrentRegion);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);

        // 加载当前配置
        _ = LoadConfigAsync();
    }

    /// <summary>
    /// 加载当前配置
    /// </summary>
    private async Task LoadConfigAsync()
    {
        try
        {
            var config = await _databaseService.GetConfigAsync();
            
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
            System.Diagnostics.Debug.WriteLine($"加载OCR配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载PDF文件
    /// </summary>
    private async Task LoadPdfAsync()
    {
        try
        {
            // 打开文件选择对话框
            var files = await _fileService.OpenFileDialogAsync("PDF Files|*.pdf", OwnerWindow);
            if (files.Length == 0) return;

            var pdfPath = files[0];
            
            // 渲染PDF第一页
            PreviewImage = await _pdfRenderService.RenderPageAsync(pdfPath, 1, 150);
            
            // 触发区域更新事件
            RegionsUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载PDF失败: {ex.Message}");
            await Views.MessageDialog.ShowErrorAsync(OwnerWindow, $"加载PDF失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新当前选择的区域
    /// </summary>
    public void UpdateCurrentRegion(float x, float y, float width, float height)
    {
        if (CurrentRegionType == "快递单号")
        {
            TrackingNumberRegion.X = x;
            TrackingNumberRegion.Y = y;
            TrackingNumberRegion.Width = width;
            TrackingNumberRegion.Height = height;
            OnPropertyChanged(nameof(TrackingNumberRegion));
        }
        else if (CurrentRegionType == "件数")
        {
            PackageCountRegion.X = x;
            PackageCountRegion.Y = y;
            PackageCountRegion.Width = width;
            PackageCountRegion.Height = height;
            OnPropertyChanged(nameof(PackageCountRegion));
        }
        
        // 触发区域更新事件
        RegionsUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 恢复当前区域的默认值
    /// </summary>
    private void ResetCurrentRegion()
    {
        if (CurrentRegionType == "快递单号")
        {
            TrackingNumberRegion = new OcrRegion
            {
                X = 0.05f,
                Y = 0.85f,
                Width = 0.5f,
                Height = 0.08f
            };
        }
        else if (CurrentRegionType == "件数")
        {
            PackageCountRegion = new OcrRegion
            {
                X = 0.7f,
                Y = 0.45f,
                Width = 0.25f,
                Height = 0.15f
            };
        }
        
        // 触发区域更新事件
        RegionsUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            // 加载现有配置
            var config = await _databaseService.GetConfigAsync();
            
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

            System.Diagnostics.Debug.WriteLine("OCR区域配置保存成功");
            
            // 关闭对话框
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存OCR配置失败: {ex.Message}");
            await Views.MessageDialog.ShowErrorAsync(OwnerWindow, $"保存配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 取消并关闭对话框
    /// </summary>
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
