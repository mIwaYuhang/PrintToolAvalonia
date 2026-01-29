using Avalonia.Media.Imaging;
using System.Drawing;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// OCR识别服务接口
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// 识别图像中指定区域的文字
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="region">识别区域（相对坐标，0-1范围）</param>
    /// <returns>识别到的文字</returns>
    Task<string> RecognizeTextAsync(Avalonia.Media.Imaging.Bitmap image, RectangleF region);
    
    /// <summary>
    /// 识别快递单号（左下角区域）
    /// </summary>
    /// <param name="image">图像</param>
    /// <returns>识别到的快递单号</returns>
    Task<string> RecognizeTrackingNumberAsync(Avalonia.Media.Imaging.Bitmap image);
    
    /// <summary>
    /// 识别件数（快递单号附近区域）
    /// </summary>
    /// <param name="image">图像</param>
    /// <returns>识别到的件数</returns>
    Task<string> RecognizePackageCountAsync(Avalonia.Media.Imaging.Bitmap image);
}
