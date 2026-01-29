using System;
using System.Collections.Generic;

namespace PrintToolAvalonia.Models;

/// <summary>
/// 打印记录
/// </summary>
public class PrintRecord
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 打印时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 电商平台
    /// </summary>
    public Platform Platform { get; set; }
    
    /// <summary>
    /// 主单文件列表
    /// </summary>
    public List<string> MainOrderFiles { get; set; } = new();
    
    /// <summary>
    /// 条码文件列表
    /// </summary>
    public List<string> BarcodeFiles { get; set; } = new();
    
    /// <summary>
    /// 环保码名称
    /// </summary>
    public string? EcoCodeName { get; set; }
    
    /// <summary>
    /// 主单数量
    /// </summary>
    public int MainOrderCount { get; set; }
    
    /// <summary>
    /// 条码数量
    /// </summary>
    public int BarcodeCount { get; set; }
    
    /// <summary>
    /// 环保码数量
    /// </summary>
    public int EcoCodeCount { get; set; }
    
    /// <summary>
    /// 打印状态
    /// </summary>
    public PrintStatus Status { get; set; }
}
