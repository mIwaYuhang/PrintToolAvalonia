namespace PrintToolAvalonia.Models;

/// <summary>
/// 应用程序配置
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 配置 ID（用于数据库）
    /// </summary>
    public string Id { get; set; } = "config";
    
    /// <summary>
    /// 主单打印机配置
    /// </summary>
    public PrinterConfig MainOrderPrinter { get; set; } = new();
    
    /// <summary>
    /// 条码打印机配置
    /// </summary>
    public PrinterConfig BarcodePrinter { get; set; } = new();
    
    /// <summary>
    /// 环保码打印机配置
    /// </summary>
    public PrinterConfig EcoCodePrinter { get; set; } = new();
    
    /// <summary>
    /// 上次使用的平台
    /// </summary>
    public Platform LastPlatform { get; set; } = Platform.TEMU;
}
