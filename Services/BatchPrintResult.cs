using System.Collections.Generic;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 批量打印结果
/// </summary>
public class BatchPrintResult
{
    /// <summary>
    /// 总任务数
    /// </summary>
    public int TotalJobs { get; set; }
    
    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }
    
    /// <summary>
    /// 失败的任务列表
    /// </summary>
    public List<(PrintJob Job, string Error)> FailedJobs { get; set; } = new();
}
