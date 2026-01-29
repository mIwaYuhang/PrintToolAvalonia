using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrintToolAvalonia.Models;
using PdfiumViewer;

namespace PrintToolAvalonia.Services;

/// <summary>
/// Windows 打印服务实现
/// </summary>
public class WindowsPrintService : IPrintService
{
    private const string UK_EU_BARCODE_RESOURCE = "uk_eu_barcode.pdf";
    
    /// <summary>
    /// 获取内置资源文件路径（如英代欧代条码）
    /// </summary>
    public string GetBuiltinResourcePath(string resourceName)
    {
        // 从应用程序资源目录获取内置文件
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "Resources", resourceName);
    }
    
    /// <summary>
    /// 获取系统打印机列表
    /// </summary>
    public Task<List<PrinterInfo>> GetPrintersAsync()
    {
        return Task.Run(() =>
        {
            var printers = new List<PrinterInfo>();
            
            try
            {
                // 获取默认打印机
                var defaultPrinter = new PrinterSettings().PrinterName;
                
                // 遍历所有已安装的打印机
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var printerInfo = new PrinterInfo
                    {
                        Name = printerName,
                        IsDefault = printerName == defaultPrinter,
                        Status = GetPrinterStatus(printerName)
                    };
                    
                    printers.Add(printerInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取打印机列表失败: {ex.Message}");
            }
            
            return printers;
        });
    }

    /// <summary>
    /// 打印 PDF 文件
    /// </summary>
    public async Task<PrintResult> PrintPdfAsync(PrintOptions options)
    {
        return await Task.Run(() =>
        {
            Exception? printException = null;
            bool printStarted = false;
            
            try
            {
                Console.WriteLine($"[PrintService] 开始打印: 文件={options.FilePath}");
                Console.WriteLine($"[PrintService] 打印机={options.PrinterName}");
                Console.WriteLine($"[PrintService] 纸张尺寸={options.PaperWidthMm}x{options.PaperHeightMm}mm");
                Console.WriteLine($"[PrintService] 份数={options.Copies}");

                // 1. 验证文件是否存在
                if (!File.Exists(options.FilePath))
                {
                    var error = $"文件不存在: {options.FilePath}";
                    Console.WriteLine($"[PrintService] 错误: {error}");
                    return new PrintResult { Success = false, Error = error };
                }

                // 2. 加载 PDF 文档
                Console.WriteLine($"[PrintService] 正在加载 PDF 文档...");
                using var document = PdfDocument.Load(options.FilePath);
                Console.WriteLine($"[PrintService] PDF 加载成功，页数: {document.PageCount}");
                
                // 3. 创建 PrintDocument
                Console.WriteLine($"[PrintService] 正在创建打印文档...");
                using var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = options.PrinterName;
                printDoc.PrinterSettings.Copies = (short)options.Copies;
                
                // 验证打印机是否有效
                if (!printDoc.PrinterSettings.IsValid)
                {
                    var error = $"打印机无效或不可用: {options.PrinterName}";
                    Console.WriteLine($"[PrintService] 错误: {error}");
                    return new PrintResult { Success = false, Error = error };
                }
                
                Console.WriteLine($"[PrintService] 打印机验证成功");
                Console.WriteLine($"[PrintService] 打印机状态: IsValid={printDoc.PrinterSettings.IsValid}");
                Console.WriteLine($"[PrintService] 打印机支持的纸张数量: {printDoc.PrinterSettings.PaperSizes.Count}");
                
                // 列出打印机支持的纸张尺寸（前10个）
                Console.WriteLine($"[PrintService] 打印机支持的纸张尺寸:");
                int count = 0;
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                {
                    if (count++ < 10)
                    {
                        Console.WriteLine($"  - {ps.PaperName}: {ps.Width}x{ps.Height} (1/100英寸)");
                    }
                }

                // 4. 设置自定义纸张大小
                // 将毫米转换为 1/100 英寸 (PrintDocument 使用的单位)
                // 1 毫米 = 3.937 * 1/100 英寸
                int widthInHundredthsOfInch = (int)(options.PaperWidthMm * 3.937);
                int heightInHundredthsOfInch = (int)(options.PaperHeightMm * 3.937);
                
                Console.WriteLine($"[PrintService] 纸张尺寸转换: {widthInHundredthsOfInch}x{heightInHundredthsOfInch} (1/100英寸)");
                
                // 尝试查找匹配的预定义纸张尺寸
                PaperSize? matchingPaper = null;
                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                {
                    // 允许 5% 的误差
                    if (Math.Abs(ps.Width - widthInHundredthsOfInch) <= widthInHundredthsOfInch * 0.05 &&
                        Math.Abs(ps.Height - heightInHundredthsOfInch) <= heightInHundredthsOfInch * 0.05)
                    {
                        matchingPaper = ps;
                        Console.WriteLine($"[PrintService] 找到匹配的预定义纸张: {ps.PaperName}");
                        break;
                    }
                }
                
                PaperSize paperSize;
                if (matchingPaper != null)
                {
                    // 使用预定义纸张
                    paperSize = matchingPaper;
                    Console.WriteLine($"[PrintService] 使用预定义纸张: {paperSize.PaperName}");
                }
                else
                {
                    // 创建自定义纸张
                    paperSize = new PaperSize("Custom", 
                        widthInHundredthsOfInch, 
                        heightInHundredthsOfInch);
                    Console.WriteLine($"[PrintService] 创建自定义纸张");
                }
                
                printDoc.DefaultPageSettings.PaperSize = paperSize;
                
                // 设置打印机的其他属性（针对热敏打印机）
                printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
                printDoc.DefaultPageSettings.Landscape = false;
                
                Console.WriteLine($"[PrintService] 纸张设置完成: {paperSize.PaperName}, {paperSize.Width}x{paperSize.Height}");
                Console.WriteLine($"[PrintService] 边距设置: 0,0,0,0");
                Console.WriteLine($"[PrintService] 横向打印: False");
                
                // 5. 设置打印页面事件
                int currentPage = 0;
                printDoc.PrintPage += (sender, e) =>
                {
                    try
                    {
                        Console.WriteLine($"[PrintService] PrintPage 事件触发 - 第 {currentPage + 1} 页");
                        
                        if (currentPage < document.PageCount && e.Graphics != null)
                        {
                            var bounds = e.PageBounds;
                            
                            Console.WriteLine($"[PrintService] 页面边界: {bounds.Width}x{bounds.Height}");
                            Console.WriteLine($"[PrintService] DPI: {e.Graphics.DpiX}x{e.Graphics.DpiY}");
                            
                            // 获取 PDF 页面的原始尺寸（以点为单位，1点 = 1/72英寸）
                            var pageSize = document.PageSizes[currentPage];
                            float pdfWidthInPoints = pageSize.Width;
                            float pdfHeightInPoints = pageSize.Height;
                            
                            Console.WriteLine($"[PrintService] PDF 原始尺寸: {pdfWidthInPoints}x{pdfHeightInPoints} 点");
                            Console.WriteLine($"[PrintService] 页面边界: {bounds.Width}x{bounds.Height} 像素");
                            Console.WriteLine($"[PrintService] DPI: {e.Graphics.DpiX}x{e.Graphics.DpiY}");
                            
                            // 直接使用用户设置的纸张尺寸（毫米）转换为点
                            // 1 毫米 = 1/25.4 英寸 = 1/25.4 * 72 点 = 2.834645669 点
                            float paperWidthInPoints = options.PaperWidthMm * 2.834645669f;
                            float paperHeightInPoints = options.PaperHeightMm * 2.834645669f;
                            
                            // 将点转换为像素（用于渲染）
                            int paperWidthInPixels = (int)(paperWidthInPoints * e.Graphics.DpiX / 72f);
                            int paperHeightInPixels = (int)(paperHeightInPoints * e.Graphics.DpiY / 72f);
                            
                            Console.WriteLine($"[PrintService] 纸张尺寸(毫米): {options.PaperWidthMm}x{options.PaperHeightMm}");
                            Console.WriteLine($"[PrintService] 纸张尺寸(点): {paperWidthInPoints:F2}x{paperHeightInPoints:F2}");
                            Console.WriteLine($"[PrintService] 纸张尺寸(像素): {paperWidthInPixels}x{paperHeightInPixels}");
                            Console.WriteLine($"[PrintService] 直接渲染到纸张尺寸");
                            
                            // 直接渲染到纸张尺寸（PdfiumViewer 会自动缩放）
                            document.Render(
                                currentPage, 
                                e.Graphics, 
                                e.Graphics.DpiX, 
                                e.Graphics.DpiY,
                                new Rectangle(0, 0, paperWidthInPixels, paperHeightInPixels),
                                PdfRenderFlags.Annotations | PdfRenderFlags.ForPrinting
                            );
                            
                            Console.WriteLine($"[PrintService] 第 {currentPage + 1} 页渲染完成");
                            
                            currentPage++;
                            e.HasMorePages = currentPage < document.PageCount;
                            
                            Console.WriteLine($"[PrintService] HasMorePages={e.HasMorePages}");
                        }
                        else
                        {
                            e.HasMorePages = false;
                            Console.WriteLine($"[PrintService] 没有更多页面");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PrintService] PrintPage 事件异常: {ex.Message}");
                        Console.WriteLine($"[PrintService] 堆栈跟踪: {ex.StackTrace}");
                        e.HasMorePages = false;
                        printException = ex;
                    }
                };
                
                // 添加 BeginPrint 事件
                printDoc.BeginPrint += (sender, e) =>
                {
                    Console.WriteLine($"[PrintService] BeginPrint 事件触发");
                    printStarted = true;
                };
                
                // 添加 EndPrint 事件
                printDoc.EndPrint += (sender, e) =>
                {
                    Console.WriteLine($"[PrintService] EndPrint 事件触发");
                    Console.WriteLine($"[PrintService] Cancelled={e.Cancel}");
                };
                
                // 6. 执行打印
                Console.WriteLine($"[PrintService] 调用 Print() 方法...");
                printDoc.Print();
                Console.WriteLine($"[PrintService] Print() 方法返回");
                
                // 检查是否有打印异常
                if (printException != null)
                {
                    return new PrintResult 
                    { 
                        Success = false, 
                        Error = $"打印过程中出错: {printException.Message}" 
                    };
                }
                
                // 检查打印是否真正开始
                if (!printStarted)
                {
                    Console.WriteLine($"[PrintService] 警告: BeginPrint 事件未触发，打印可能未开始");
                    return new PrintResult 
                    { 
                        Success = false, 
                        Error = "打印任务未能启动，请检查打印机状态和驱动程序" 
                    };
                }
                
                Console.WriteLine($"[PrintService] 打印完成");
                return new PrintResult { Success = true };
            }
            catch (Exception ex)
            {
                var error = $"打印失败: {ex.Message}";
                Console.WriteLine($"[PrintService] 异常: {error}");
                Console.WriteLine($"[PrintService] 异常类型: {ex.GetType().Name}");
                Console.WriteLine($"[PrintService] 堆栈跟踪: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[PrintService] 内部异常: {ex.InnerException.Message}");
                }
                
                return new PrintResult 
                { 
                    Success = false, 
                    Error = error
                };
            }
        });
    }

    /// <summary>
    /// 批量打印
    /// </summary>
    public async Task<BatchPrintResult> PrintBatchAsync(
        List<PrintJob> jobs, 
        IProgress<int>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var result = new BatchPrintResult
        {
            TotalJobs = jobs.Count
        };

        for (int i = 0; i < jobs.Count; i++)
        {
            // 检查取消请求
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var job = jobs[i];
            
            try
            {
                var printResult = await PrintPdfAsync(job.Options);
                
                if (printResult.Success)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.FailedJobs.Add((job, printResult.Error ?? "未知错误"));
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedJobs.Add((job, ex.Message));
            }

            // 报告进度
            progress?.Report(i + 1);
        }

        return result;
    }

    /// <summary>
    /// 获取打印机状态
    /// </summary>
    private PrinterStatus GetPrinterStatus(string printerName)
    {
        try
        {
            var settings = new PrinterSettings { PrinterName = printerName };
            
            // 检查打印机是否有效
            if (!settings.IsValid)
            {
                return PrinterStatus.Offline;
            }

            // 简单判断：如果打印机有效，认为就绪
            // 更详细的状态检查需要使用 WMI 或 Win32 API
            return PrinterStatus.Ready;
        }
        catch
        {
            return PrinterStatus.Error;
        }
    }
}
