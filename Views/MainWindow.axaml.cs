using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PrintToolAvalonia.ViewModels;
using System.Linq;

namespace PrintToolAvalonia.Views;

public partial class MainWindow : Window
{
    private Border? _mainOrderDropZone;
    private Border? _barcodeDropZone;
    private IBrush? _originalMainOrderBackground;
    private IBrush? _originalBarcodeBackground;
    private IBrush? _originalMainOrderBorder;
    private IBrush? _originalBarcodeBorder;

    public MainWindow()
    {
        InitializeComponent();
        
        // 订阅 Loaded 事件以获取拖放区域的引用
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 设置 EcoCodeViewModel 的父窗口引用
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.EcoCodeViewModel.OwnerWindow = this;
        }

        // 获取拖放区域的引用
        _mainOrderDropZone = this.FindControl<Border>("MainOrderDropZone");
        _barcodeDropZone = this.FindControl<Border>("BarcodeDropZone");

        if (_mainOrderDropZone != null)
        {
            // 保存原始样式
            _originalMainOrderBackground = _mainOrderDropZone.Background;
            _originalMainOrderBorder = _mainOrderDropZone.BorderBrush;

            // 注册拖放事件
            _mainOrderDropZone.AddHandler(DragDrop.DragEnterEvent, OnMainOrderDragEnter);
            _mainOrderDropZone.AddHandler(DragDrop.DragLeaveEvent, OnMainOrderDragLeave);
            _mainOrderDropZone.AddHandler(DragDrop.DragOverEvent, OnMainOrderDragOver);
            _mainOrderDropZone.AddHandler(DragDrop.DropEvent, OnMainOrderDrop);
        }

        if (_barcodeDropZone != null)
        {
            // 保存原始样式
            _originalBarcodeBackground = _barcodeDropZone.Background;
            _originalBarcodeBorder = _barcodeDropZone.BorderBrush;

            // 注册拖放事件
            _barcodeDropZone.AddHandler(DragDrop.DragEnterEvent, OnBarcodeDragEnter);
            _barcodeDropZone.AddHandler(DragDrop.DragLeaveEvent, OnBarcodeDragLeave);
            _barcodeDropZone.AddHandler(DragDrop.DragOverEvent, OnBarcodeDragOver);
            _barcodeDropZone.AddHandler(DragDrop.DropEvent, OnBarcodeDrop);
        }
    }

    #region 主单拖放事件

    private void OnMainOrderDragEnter(object? sender, DragEventArgs e)
    {
        // 检查是否包含文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && files.Any(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)))
            {
                // 应用悬停样式
                if (_mainOrderDropZone != null)
                {
                    _mainOrderDropZone.Classes.Add("drop-zone-hover");
                }
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void OnMainOrderDragLeave(object? sender, DragEventArgs e)
    {
        // 移除悬停样式
        if (_mainOrderDropZone != null)
        {
            _mainOrderDropZone.Classes.Remove("drop-zone-hover");
        }
        
        e.Handled = true;
    }

    private void OnMainOrderDragOver(object? sender, DragEventArgs e)
    {
        // 检查是否包含 PDF 文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && files.Any(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private async void OnMainOrderDrop(object? sender, DragEventArgs e)
    {
        // 移除悬停样式
        if (_mainOrderDropZone != null)
        {
            _mainOrderDropZone.Classes.Remove("drop-zone-hover");
        }

        // 获取拖放的文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                // 过滤 PDF 文件
                var pdfFiles = files
                    .Where(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Path.LocalPath)
                    .ToArray();

                // 将文件路径传递给 ViewModel
                if (pdfFiles.Length > 0 && DataContext is MainWindowViewModel viewModel)
                {
                    await viewModel.AddFilesAsync(pdfFiles, "MainOrder");
                }
            }
        }
        
        e.Handled = true;
    }

    #endregion

    #region 条码拖放事件

    private void OnBarcodeDragEnter(object? sender, DragEventArgs e)
    {
        // 检查是否包含文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && files.Any(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)))
            {
                // 应用悬停样式
                if (_barcodeDropZone != null)
                {
                    _barcodeDropZone.Classes.Add("drop-zone-hover");
                }
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void OnBarcodeDragLeave(object? sender, DragEventArgs e)
    {
        // 移除悬停样式
        if (_barcodeDropZone != null)
        {
            _barcodeDropZone.Classes.Remove("drop-zone-hover");
        }
        
        e.Handled = true;
    }

    private void OnBarcodeDragOver(object? sender, DragEventArgs e)
    {
        // 检查是否包含 PDF 文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && files.Any(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private async void OnBarcodeDrop(object? sender, DragEventArgs e)
    {
        // 移除悬停样式
        if (_barcodeDropZone != null)
        {
            _barcodeDropZone.Classes.Remove("drop-zone-hover");
        }

        // 获取拖放的文件
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                // 过滤 PDF 文件
                var pdfFiles = files
                    .Where(f => f.Path.LocalPath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Path.LocalPath)
                    .ToArray();

                // 将文件路径传递给 ViewModel
                if (pdfFiles.Length > 0 && DataContext is MainWindowViewModel viewModel)
                {
                    await viewModel.AddFilesAsync(pdfFiles, "Barcode");
                }
            }
        }
        
        e.Handled = true;
    }

    #endregion
}