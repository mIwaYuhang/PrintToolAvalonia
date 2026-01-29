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
    
    /// <summary>
    /// 快递单号OCR识别区域（相对坐标）
    /// </summary>
    public OcrRegion TrackingNumberRegion { get; set; } = new()
    {
        X = 0.05f,
        Y = 0.85f,
        Width = 0.5f,
        Height = 0.08f
    };
    
    /// <summary>
    /// 件数OCR识别区域（相对坐标）
    /// </summary>
    public OcrRegion PackageCountRegion { get; set; } = new()
    {
        X = 0.7f,
        Y = 0.45f,
        Width = 0.25f,
        Height = 0.15f
    };
}
