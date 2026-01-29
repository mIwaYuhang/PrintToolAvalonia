using System;
using Avalonia.Controls;
using PrintToolAvalonia.ViewModels;

namespace PrintToolAvalonia.Views;

public partial class HistoryDialog : Window
{
    public HistoryDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // 订阅 ViewModel 的关闭请求事件
        if (DataContext is HistoryViewModel viewModel)
        {
            viewModel.RequestClose += (s, args) => Close();
        }
    }
}
