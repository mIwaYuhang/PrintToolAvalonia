using Avalonia.Controls;
using PrintToolAvalonia.ViewModels;
using System;

namespace PrintToolAvalonia.Views;

/// <summary>
/// 标签选择对话框
/// </summary>
public partial class LabelSelectionDialog : Window
{
    public LabelSelectionDialog()
    {
        InitializeComponent();
        
        // 订阅关闭事件
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LabelSelectionViewModel viewModel)
        {
            viewModel.CloseRequested += (s, args) => Close();
        }
    }
}
