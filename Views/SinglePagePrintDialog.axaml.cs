using System;
using Avalonia.Controls;
using PrintToolAvalonia.ViewModels;

namespace PrintToolAvalonia.Views;

/// <summary>
/// 单页打印对话框
/// </summary>
public partial class SinglePagePrintDialog : Window
{
    public SinglePagePrintDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // 设置ViewModel的OwnerWindow属性
        if (DataContext is SinglePagePrintViewModel viewModel)
        {
            viewModel.OwnerWindow = this;
        }
    }
}
