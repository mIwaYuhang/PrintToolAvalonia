using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 图像匹配服务接口
/// </summary>
public interface IImageMatchService
{
    /// <summary>
    /// 加载模板图像
    /// </summary>
    /// <param name="templatePath">模板图像路径</param>
    void LoadTemplate(string templatePath);
    
    /// <summary>
    /// 检测图像中是否包含模板
    /// </summary>
    /// <param name="image">待检测图像</param>
    /// <param name="threshold">相似度阈值（0-1，默认0.5）</param>
    /// <returns>是否匹配</returns>
    Task<bool> MatchTemplateAsync(Bitmap image, double threshold = 0.5);
    
    /// <summary>
    /// 扫描PDF所有页面，识别分隔符位置
    /// </summary>
    /// <param name="pdfFilePath">PDF文件路径</param>
    /// <param name="threshold">相似度阈值</param>
    /// <returns>分隔符页码列表（从1开始）</returns>
    Task<List<int>> ScanSeparatorsAsync(string pdfFilePath, double threshold = 0.5);
}
