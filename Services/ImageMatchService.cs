using Avalonia.Media.Imaging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 图像匹配服务实现
/// 使用OpenCV进行模板匹配
/// </summary>
public class ImageMatchService : IImageMatchService
{
    private Mat? _templateMat;
    private readonly IPdfRenderService _pdfRenderService;
    
    public ImageMatchService(IPdfRenderService pdfRenderService)
    {
        _pdfRenderService = pdfRenderService;
    }
    
    /// <summary>
    /// 加载模板图像并转换为灰度图
    /// </summary>
    public void LoadTemplate(string templatePath)
    {
        Console.WriteLine($"[ImageMatchService] 正在加载模板图像: {templatePath}");
        
        if (!File.Exists(templatePath))
        {
            Console.WriteLine($"[ImageMatchService] 模板文件不存在!");
            throw new FileNotFoundException($"无法找到模板图像: {templatePath}");
        }
        
        // 加载模板图像并转换为灰度图
        _templateMat = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
        
        if (_templateMat == null || _templateMat.Empty())
        {
            Console.WriteLine($"[ImageMatchService] 模板图像加载失败!");
            throw new InvalidOperationException($"无法加载模板图像: {templatePath}");
        }
        
        Console.WriteLine($"[ImageMatchService] 模板图像加载成功，尺寸: {_templateMat.Width}x{_templateMat.Height}");
    }
    
    /// <summary>
    /// 检测图像中是否包含模板
    /// 使用多尺度模板匹配以适应不同大小的分隔符
    /// </summary>
    public async Task<bool> MatchTemplateAsync(Bitmap image, double threshold = 0.5)
    {
        if (_templateMat == null)
        {
            throw new InvalidOperationException("请先调用LoadTemplate加载模板");
        }
        
        return await Task.Run(() =>
        {
            try
            {
                // 将Avalonia Bitmap转换为OpenCV Mat
                using var sourceMat = BitmapToMat(image);
                
                // 转换为灰度图
                using var grayMat = new Mat();
                Cv2.CvtColor(sourceMat, grayMat, ColorConversionCodes.BGR2GRAY);
                
                double maxScore = 0.0;
                
                // 尝试多个缩放比例进行匹配（0.5x, 0.75x, 1x, 1.25x, 1.5x, 2x）
                var scales = new[] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
                
                foreach (var scale in scales)
                {
                    try
                    {
                        Mat templateToUse;
                        
                        if (Math.Abs(scale - 1.0) < 0.01)
                        {
                            // 使用原始模板
                            templateToUse = _templateMat;
                        }
                        else
                        {
                            // 缩放模板
                            templateToUse = new Mat();
                            var newSize = new OpenCvSharp.Size(
                                (int)(_templateMat.Width * scale),
                                (int)(_templateMat.Height * scale)
                            );
                            Cv2.Resize(_templateMat, templateToUse, newSize);
                        }
                        
                        // 检查模板是否小于源图像
                        if (templateToUse.Width > grayMat.Width || templateToUse.Height > grayMat.Height)
                        {
                            if (Math.Abs(scale - 1.0) >= 0.01)
                            {
                                templateToUse.Dispose();
                            }
                            continue;
                        }
                        
                        // 执行模板匹配
                        using var result = new Mat();
                        Cv2.MatchTemplate(grayMat, templateToUse, result, TemplateMatchModes.CCoeffNormed);
                        
                        // 获取最大匹配值
                        Cv2.MinMaxLoc(result, out _, out double maxVal);
                        
                        if (maxVal > maxScore)
                        {
                            maxScore = maxVal;
                        }
                        
                        Console.WriteLine($"[ImageMatchService]   尺度 {scale:F2}x: 匹配分数 {maxVal:F4}");
                        
                        // 清理缩放后的模板
                        if (Math.Abs(scale - 1.0) >= 0.01)
                        {
                            templateToUse.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ImageMatchService]   尺度 {scale:F2}x 匹配失败: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"[ImageMatchService] 最佳匹配分数: {maxScore:F4} (阈值: {threshold})");
                
                // 判断是否超过阈值
                return maxScore >= threshold;
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出，返回false表示未匹配
                Console.WriteLine($"[ImageMatchService] 模板匹配失败: {ex.Message}");
                Console.WriteLine($"[ImageMatchService] 错误堆栈: {ex.StackTrace}");
                return false;
            }
        });
    }
    
    /// <summary>
    /// 扫描PDF所有页面，识别分隔符位置
    /// </summary>
    public async Task<List<int>> ScanSeparatorsAsync(string pdfFilePath, double threshold = 0.5)
    {
        var separatorPages = new List<int>();
        
        try
        {
            Console.WriteLine($"[ImageMatchService] 开始扫描PDF: {pdfFilePath}");
            Console.WriteLine($"[ImageMatchService] 匹配阈值: {threshold}");
            
            var pageCount = await _pdfRenderService.GetPageCountAsync(pdfFilePath);
            Console.WriteLine($"[ImageMatchService] PDF总页数: {pageCount}");
            
            for (int page = 1; page <= pageCount; page++)
            {
                try
                {
                    Console.WriteLine($"[ImageMatchService] 正在扫描第 {page}/{pageCount} 页...");
                    
                    // 渲染页面（使用150 DPI以平衡性能和准确性）
                    var pageImage = await _pdfRenderService.RenderPageAsync(pdfFilePath, page, 150);
                    
                    if (pageImage == null)
                    {
                        Console.WriteLine($"[ImageMatchService] 第 {page} 页渲染失败，跳过");
                        continue;
                    }
                    
                    Console.WriteLine($"[ImageMatchService] 第 {page} 页渲染成功，开始模板匹配...");
                    
                    // 检测是否为分隔符
                    var isMatch = await MatchTemplateAsync(pageImage, threshold);
                    
                    Console.WriteLine($"[ImageMatchService] 第 {page} 页匹配结果: {(isMatch ? "匹配成功 ✓" : "不匹配")}");
                    
                    if (isMatch)
                    {
                        separatorPages.Add(page);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但继续扫描其他页面
                    Console.WriteLine($"[ImageMatchService] 扫描第{page}页时出错: {ex.Message}");
                    Console.WriteLine($"[ImageMatchService] 错误堆栈: {ex.StackTrace}");
                }
            }
            
            Console.WriteLine($"[ImageMatchService] 扫描完成，共找到 {separatorPages.Count} 个分隔符页面");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageMatchService] 扫描PDF失败: {ex.Message}");
            Console.WriteLine($"[ImageMatchService] 错误堆栈: {ex.StackTrace}");
        }
        
        return separatorPages;
    }
    
    /// <summary>
    /// 将Avalonia Bitmap转换为OpenCV Mat
    /// 通过MemoryStream作为中间格式
    /// </summary>
    private Mat BitmapToMat(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        
        // 将Avalonia Bitmap保存为PNG到内存流
        bitmap.Save(memoryStream);
        memoryStream.Position = 0;
        
        // 从内存流读取为字节数组
        var imageBytes = memoryStream.ToArray();
        
        // 使用OpenCV从字节数组解码图像
        var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        
        if (mat == null || mat.Empty())
        {
            throw new InvalidOperationException("无法将Bitmap转换为Mat");
        }
        
        return mat;
    }
}
