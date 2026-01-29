using Avalonia.Media.Imaging;
using PdfiumViewer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// PDF渲染服务实现
/// </summary>
public class PdfRenderService : IPdfRenderService
{
    /// <summary>
    /// 获取PDF文件的总页数
    /// </summary>
    public async Task<int> GetPageCountAsync(string pdfFilePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 验证文件是否存在
                if (!File.Exists(pdfFilePath))
                {
                    throw new FileNotFoundException($"PDF文件不存在: {pdfFilePath}");
                }
                
                using var document = PdfDocument.Load(pdfFilePath);
                return document.PageCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取PDF页数失败: {ex.Message}", ex);
            }
        });
    }
    
    /// <summary>
    /// 渲染指定页面为图像
    /// </summary>
    public async Task<Bitmap?> RenderPageAsync(string pdfFilePath, int pageNumber, int dpi = 150)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 验证文件是否存在
                if (!File.Exists(pdfFilePath))
                {
                    throw new FileNotFoundException($"PDF文件不存在: {pdfFilePath}");
                }
                
                using var document = PdfDocument.Load(pdfFilePath);
                
                // 页码从0开始（内部使用）
                int pageIndex = pageNumber - 1;
                
                // 验证页码是否有效
                if (pageIndex < 0 || pageIndex >= document.PageCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(pageNumber),
                        $"页码超出范围。有效范围: 1-{document.PageCount}，实际值: {pageNumber}");
                }
                
                // 获取页面尺寸
                var pageSize = document.PageSizes[pageIndex];
                int width = (int)(pageSize.Width * dpi / 72);
                int height = (int)(pageSize.Height * dpi / 72);
                
                // 渲染页面为System.Drawing.Image
                using var image = document.Render(pageIndex, width, height, dpi, dpi, false);
                
                // 转换为Avalonia Bitmap
                return ConvertToAvaloniaBitmap(image);
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException($"渲染PDF页面失败: {ex.Message}", ex);
            }
        });
    }
    
    /// <summary>
    /// 将System.Drawing.Image转换为Avalonia.Media.Imaging.Bitmap
    /// </summary>
    private Bitmap ConvertToAvaloniaBitmap(System.Drawing.Image image)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            
            // 将System.Drawing.Image保存到内存流（使用PNG格式保证质量）
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            
            // 从内存流创建Avalonia Bitmap
            return new Bitmap(memoryStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"图像格式转换失败: {ex.Message}", ex);
        }
    }
}
