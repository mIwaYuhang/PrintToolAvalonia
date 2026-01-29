namespace PrintToolAvalonia.Services;

/// <summary>
/// 打印任务
/// </summary>
public class PrintJob
{
    /// <summary>
    /// 打印选项
    /// </summary>
    public PrintOptions Options { get; set; } = new();
    
    /// <summary>
    /// 任务描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
