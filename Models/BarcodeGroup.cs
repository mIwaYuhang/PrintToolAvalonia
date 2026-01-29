using Avalonia.Media.Imaging;
using System;

namespace PrintToolAvalonia.Models;

/// <summary>
/// 条码分组模型
/// 表示由分隔符截断的一组连续条码页面
/// </summary>
public class BarcodeGroup
{
    /// <summary>
    /// 分组ID（唯一标识）
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 起始页码（从1开始，包含）
    /// </summary>
    public int StartPage { get; set; }
    
    /// <summary>
    /// 结束页码（从1开始，包含）
    /// </summary>
    public int EndPage { get; set; }
    
    /// <summary>
    /// 条码数量（计算属性）
    /// 等于 EndPage - StartPage + 1
    /// </summary>
    public int BarcodeCount => EndPage - StartPage + 1;
    
    /// <summary>
    /// 最后一页的预览图
    /// </summary>
    public Bitmap? PreviewImage { get; set; }
    
    /// <summary>
    /// 是否已打印
    /// </summary>
    public bool IsPrinted { get; set; }
    
    /// <summary>
    /// 打印时间
    /// </summary>
    public DateTime? PrintedAt { get; set; }
}
