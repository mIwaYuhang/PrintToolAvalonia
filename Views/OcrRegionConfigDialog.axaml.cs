using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PrintToolAvalonia.ViewModels;
using System;

namespace PrintToolAvalonia.Views;

public partial class OcrRegionConfigDialog : Window
{
    private Point _startPoint;
    private bool _isDragging;
    private Canvas? _canvas;
    private Border? _trackingNumberBorder;
    private Border? _packageCountBorder;
    private Image? _previewImage;

    public OcrRegionConfigDialog()
    {
        InitializeComponent();
        
        // 获取控件引用
        _canvas = this.FindControl<Canvas>("SelectionCanvas");
        _trackingNumberBorder = this.FindControl<Border>("TrackingNumberRegionBorder");
        _packageCountBorder = this.FindControl<Border>("PackageCountRegionBorder");
        _previewImage = this.FindControl<Image>("PreviewImage");
        
        // 监听ViewModel变化以更新UI
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is OcrRegionConfigViewModel viewModel)
        {
            viewModel.RequestClose += (s, args) => Close();
            viewModel.RegionsUpdated += OnRegionsUpdated;
        }
    }

    /// <summary>
    /// 当区域更新时，重新绘制选择框
    /// </summary>
    private void OnRegionsUpdated(object? sender, EventArgs e)
    {
        UpdateRegionBorders();
    }

    /// <summary>
    /// 更新区域边框位置和大小
    /// </summary>
    private void UpdateRegionBorders()
    {
        if (DataContext is not OcrRegionConfigViewModel viewModel) return;
        if (_canvas == null || _previewImage == null) return;
        if (_previewImage.Source == null) return;

        // 获取图像的实际显示尺寸和位置（图像可能居中显示）
        var imageWidth = _previewImage.Bounds.Width;
        var imageHeight = _previewImage.Bounds.Height;
        var imageLeft = (_canvas.Bounds.Width - imageWidth) / 2;
        var imageTop = (_canvas.Bounds.Height - imageHeight) / 2;

        if (imageWidth <= 0 || imageHeight <= 0) return;

        // 更新快递单号区域
        if (_trackingNumberBorder != null && viewModel.TrackingNumberRegion != null)
        {
            var region = viewModel.TrackingNumberRegion;
            Canvas.SetLeft(_trackingNumberBorder, imageLeft + region.X * imageWidth);
            Canvas.SetTop(_trackingNumberBorder, imageTop + region.Y * imageHeight);
            _trackingNumberBorder.Width = region.Width * imageWidth;
            _trackingNumberBorder.Height = region.Height * imageHeight;
        }

        // 更新件数区域
        if (_packageCountBorder != null && viewModel.PackageCountRegion != null)
        {
            var region = viewModel.PackageCountRegion;
            Canvas.SetLeft(_packageCountBorder, imageLeft + region.X * imageWidth);
            Canvas.SetTop(_packageCountBorder, imageTop + region.Y * imageHeight);
            _packageCountBorder.Width = region.Width * imageWidth;
            _packageCountBorder.Height = region.Height * imageHeight;
        }
    }

    /// <summary>
    /// 鼠标按下开始拖拽
    /// </summary>
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not OcrRegionConfigViewModel viewModel) return;
        if (_canvas == null || _previewImage == null) return;
        if (_previewImage.Source == null) return;

        _startPoint = e.GetPosition(_canvas);
        _isDragging = true;
        e.Pointer.Capture(_canvas);
    }

    /// <summary>
    /// 鼠标移动更新选择区域
    /// </summary>
    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;
        if (DataContext is not OcrRegionConfigViewModel viewModel) return;
        if (_canvas == null || _previewImage == null) return;

        var currentPoint = e.GetPosition(_canvas);
        
        // 获取图像的实际显示尺寸和位置
        var imageWidth = _previewImage.Bounds.Width;
        var imageHeight = _previewImage.Bounds.Height;
        var imageLeft = (_canvas.Bounds.Width - imageWidth) / 2;
        var imageTop = (_canvas.Bounds.Height - imageHeight) / 2;

        if (imageWidth <= 0 || imageHeight <= 0) return;

        // 限制拖拽范围在图像内
        var clampedStartX = Math.Max(imageLeft, Math.Min(_startPoint.X, imageLeft + imageWidth));
        var clampedStartY = Math.Max(imageTop, Math.Min(_startPoint.Y, imageTop + imageHeight));
        var clampedCurrentX = Math.Max(imageLeft, Math.Min(currentPoint.X, imageLeft + imageWidth));
        var clampedCurrentY = Math.Max(imageTop, Math.Min(currentPoint.Y, imageTop + imageHeight));
        
        // 计算选择区域（相对于图像）
        var x = Math.Min(clampedStartX, clampedCurrentX) - imageLeft;
        var y = Math.Min(clampedStartY, clampedCurrentY) - imageTop;
        var width = Math.Abs(clampedCurrentX - clampedStartX);
        var height = Math.Abs(clampedCurrentY - clampedStartY);

        // 转换为相对坐标（0-1范围）
        var relativeX = (float)(x / imageWidth);
        var relativeY = (float)(y / imageHeight);
        var relativeWidth = (float)(width / imageWidth);
        var relativeHeight = (float)(height / imageHeight);

        // 确保在有效范围内
        relativeX = Math.Max(0, Math.Min(1, relativeX));
        relativeY = Math.Max(0, Math.Min(1, relativeY));
        relativeWidth = Math.Max(0.01f, Math.Min(1 - relativeX, relativeWidth));
        relativeHeight = Math.Max(0.01f, Math.Min(1 - relativeY, relativeHeight));

        // 更新当前选择的区域
        viewModel.UpdateCurrentRegion(relativeX, relativeY, relativeWidth, relativeHeight);
        
        // 更新UI
        UpdateRegionBorders();
    }

    /// <summary>
    /// 鼠标释放完成拖拽
    /// </summary>
    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        // 窗口打开后更新区域显示
        UpdateRegionBorders();
    }
}
