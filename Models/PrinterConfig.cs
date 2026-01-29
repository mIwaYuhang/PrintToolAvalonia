namespace PrintToolAvalonia.Models;

/// <summary>
/// 打印机配置
/// </summary>
public class PrinterConfig
{
    /// <summary>
    /// 打印机名称
    /// </summary>
    public string PrinterName { get; set; } = string.Empty;
    
    /// <summary>
    /// 纸张宽度（毫米）
    /// </summary>
    public int PaperWidthMm { get; set; }
    
    /// <summary>
    /// 纸张高度（毫米）
    /// </summary>
    public int PaperHeightMm { get; set; }
}
