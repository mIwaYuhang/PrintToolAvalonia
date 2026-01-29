using System;
using System.Collections.Generic;

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
    
    /// <summary>
    /// 页码范围（格式："1" 或 "1-3" 或 "1,3,5" 或 "1-3,5,7-9"）
    /// 如果为空或null，则打印所有页面
    /// </summary>
    public string? PageRange { get; set; }
    
    /// <summary>
    /// 解析页码范围字符串，返回页码列表
    /// </summary>
    /// <returns>页码列表（从1开始），如果PageRange为空则返回空列表</returns>
    public List<int> ParsePageRange()
    {
        var pages = new List<int>();
        
        // 如果PageRange为空或null，返回空列表（表示打印所有页）
        if (string.IsNullOrWhiteSpace(PageRange))
        {
            return pages;
        }
        
        // 按逗号分割各个部分
        var parts = PageRange.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            
            // 检查是否是范围格式（如 "1-3"）
            if (trimmedPart.Contains('-'))
            {
                var rangeParts = trimmedPart.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (rangeParts.Length == 2 &&
                    int.TryParse(rangeParts[0].Trim(), out int start) &&
                    int.TryParse(rangeParts[1].Trim(), out int end))
                {
                    // 添加范围内的所有页码
                    for (int i = start; i <= end; i++)
                    {
                        if (i > 0 && !pages.Contains(i))
                        {
                            pages.Add(i);
                        }
                    }
                }
            }
            // 单个页码
            else if (int.TryParse(trimmedPart, out int pageNumber))
            {
                if (pageNumber > 0 && !pages.Contains(pageNumber))
                {
                    pages.Add(pageNumber);
                }
            }
        }
        
        // 排序页码列表
        pages.Sort();
        
        return pages;
    }
}
