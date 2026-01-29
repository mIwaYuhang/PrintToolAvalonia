using System;

namespace PrintToolAvalonia.Models;

/// <summary>
/// 导入的文件
/// </summary>
public class ImportedFile
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件路径
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// PDF 页数
    /// </summary>
    public int PageCount { get; set; }
    
    /// <summary>
    /// 添加时间
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
