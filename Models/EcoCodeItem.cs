using System;

namespace PrintToolAvalonia.Models;

/// <summary>
/// 环保码项
/// </summary>
public class EcoCodeItem
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 环保码名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件名（存储在应用数据目录中）
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
