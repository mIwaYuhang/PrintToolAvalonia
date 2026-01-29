namespace PrintToolAvalonia.Models;

/// <summary>
/// 打印机状态枚举
/// </summary>
public enum PrinterStatus
{
    /// <summary>
    /// 就绪
    /// </summary>
    Ready,
    
    /// <summary>
    /// 离线
    /// </summary>
    Offline,
    
    /// <summary>
    /// 繁忙
    /// </summary>
    Busy,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error
}
