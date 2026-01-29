using System;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 日期过滤器
/// </summary>
public class DateFilter
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }
}
