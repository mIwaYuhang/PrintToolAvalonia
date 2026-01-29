using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 打印机信息
/// </summary>
public class PrinterInfo
{
    /// <summary>
    /// 打印机名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为默认打印机
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// 打印机状态
    /// </summary>
    public PrinterStatus Status { get; set; }
}
