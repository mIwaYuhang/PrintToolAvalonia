using System;
using Avalonia.Controls;
using PrintToolAvalonia.ViewModels;

namespace PrintToolAvalonia.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // 订阅 ViewModel 的关闭请求事件
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.RequestClose += (s, args) => Close();
            viewModel.OwnerWindow = this;
        }
    }
}
