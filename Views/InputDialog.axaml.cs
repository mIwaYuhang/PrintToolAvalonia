using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PrintToolAvalonia.Views;

/// <summary>
/// 输入对话框
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// 对话框消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// 输入的文本
    /// </summary>
    public string InputText { get; set; } = string.Empty;

    /// <summary>
    /// 对话框结果
    /// </summary>
    public bool DialogResult { get; private set; }

    public InputDialog()
    {
        InitializeComponent();
        
        // 窗口加载后设置焦点到输入框
        Loaded += (s, e) =>
        {
            var messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            var inputTextBox = this.FindControl<TextBox>("InputTextBox");
            
            if (messageTextBlock != null)
            {
                messageTextBlock.Text = Message;
            }
            
            if (inputTextBox != null)
            {
                inputTextBox.Text = InputText;
                inputTextBox.Watermark = Placeholder;
                inputTextBox.Focus();
            }
        };
    }

    /// <summary>
    /// 确定按钮点击事件
    /// </summary>
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        if (inputTextBox != null)
        {
            InputText = inputTextBox.Text ?? string.Empty;
        }
        
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    /// <param name="owner">父窗口</param>
    /// <param name="message">提示消息</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="placeholder">占位符</param>
    /// <returns>用户输入的文本，如果取消则返回 null</returns>
    public static async Task<string?> ShowAsync(
        Window? owner,
        string message,
        string defaultValue = "",
        string placeholder = "")
    {
        var dialog = new InputDialog
        {
            Message = message,
            InputText = defaultValue,
            Placeholder = placeholder
        };

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return dialog.DialogResult ? dialog.InputText : null;
    }
}
