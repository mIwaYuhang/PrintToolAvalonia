using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PrintToolAvalonia.Views;

/// <summary>
/// 消息对话框
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    /// 对话框消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public MessageDialog()
    {
        InitializeComponent();
        
        // 窗口加载后设置消息
        Loaded += (s, e) =>
        {
            var messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            if (messageTextBlock != null)
            {
                messageTextBlock.Text = Message;
            }
        };
    }

    /// <summary>
    /// 确定按钮点击事件
    /// </summary>
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 显示错误消息对话框
    /// </summary>
    public static async Task ShowErrorAsync(Window? owner, string message)
    {
        var dialog = new MessageDialog
        {
            Title = "错误",
            Message = message
        };

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    /// <summary>
    /// 显示信息消息对话框
    /// </summary>
    public static async Task ShowInfoAsync(Window? owner, string message)
    {
        var dialog = new MessageDialog
        {
            Title = "提示",
            Message = message
        };

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }
}
