using Avalonia.Controls;
using System.Threading;

namespace PrintToolAvalonia.Views;

public partial class ProgressDialog : Window
{
    private CancellationTokenSource? _cancellationTokenSource;

    public ProgressDialog()
    {
        InitializeComponent();
        
        // 订阅取消按钮点击事件
        CancelButton.Click += (s, e) => Cancel();
    }

    /// <summary>
    /// 设置取消令牌源
    /// </summary>
    public void SetCancellationTokenSource(CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(int value)
    {
        ProgressBar.Value = value;
        ProgressText.Text = $"{value}%";
    }

    /// <summary>
    /// 取消操作
    /// </summary>
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        Close();
    }
}
