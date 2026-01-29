namespace PrintToolAvalonia.Services;

/// <summary>
/// 打印选项
/// </summary>
public class PrintOptions
{
    /// <summary>
    /// PDF 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
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
    
    /// <summary>
    /// 打印份数
    /// </summary>
    public int Copies { get; set; } = 1;
}
