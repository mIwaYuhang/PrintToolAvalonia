using Avalonia.Media.Imaging;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// PDF渲染服务接口
/// </summary>
public interface IPdfRenderService
{
    /// <summary>
    /// 获取PDF文件的总页数
    /// </summary>
    /// <param name="pdfFilePath">PDF文件路径</param>
    /// <returns>总页数</returns>
    Task<int> GetPageCountAsync(string pdfFilePath);
    
    /// <summary>
    /// 渲染指定页面为图像
    /// </summary>
    /// <param name="pdfFilePath">PDF文件路径</param>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="dpi">渲染DPI（默认150）</param>
    /// <returns>渲染后的位图</returns>
    Task<Bitmap?> RenderPageAsync(string pdfFilePath, int pageNumber, int dpi = 150);
}
