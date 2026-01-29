using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 打印服务接口
/// </summary>
public interface IPrintService
{
    /// <summary>
    /// 获取系统打印机列表
    /// </summary>
    Task<List<PrinterInfo>> GetPrintersAsync();
    
    /// <summary>
    /// 打印 PDF 文件
    /// </summary>
    /// <param name="options">打印选项</param>
    /// <returns>打印结果</returns>
    Task<PrintResult> PrintPdfAsync(PrintOptions options);
    
    /// <summary>
    /// 批量打印
    /// </summary>
    /// <param name="jobs">打印任务列表</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量打印结果</returns>
    Task<BatchPrintResult> PrintBatchAsync(
        List<PrintJob> jobs, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取内置资源文件路径（如英代欧代条码）
    /// </summary>
    /// <param name="resourceName">资源文件名</param>
    /// <returns>资源文件的完整路径</returns>
    string GetBuiltinResourcePath(string resourceName);
}
