using System.Drawing;

namespace PrintToolAvalonia.Models;

/// <summary>
/// OCR识别区域（使用相对坐标）
/// </summary>
public class OcrRegion
{
    /// <summary>
    /// X坐标（相对，0-1）
    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// Y坐标（相对，0-1）
    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// 宽度（相对，0-1）
    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// 高度（相对，0-1）
    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// 转换为绝对坐标矩形
    /// </summary>
    /// <param name="imageWidth">图像宽度</param>
    /// <param name="imageHeight">图像高度</param>
    /// <returns>绝对坐标矩形</returns>
    public Rectangle ToAbsoluteRectangle(int imageWidth, int imageHeight)
    {
        return new Rectangle(
            (int)(X * imageWidth),
            (int)(Y * imageHeight),
            (int)(Width * imageWidth),
            (int)(Height * imageHeight)
        );
    }
    
    /// <summary>
    /// 转换为RectangleF
    /// </summary>
    /// <returns>相对坐标矩形</returns>
    public RectangleF ToRectangleF()
    {
        return new RectangleF(X, Y, Width, Height);
    }
}
