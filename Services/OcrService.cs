using Avalonia.Media.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;

namespace PrintToolAvalonia.Services;

/// <summary>
/// OCR识别服务实现
/// </summary>
public class OcrService : IOcrService
{
    private readonly string _tessDataPath;
    private readonly IDatabaseService _databaseService;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="databaseService">数据库服务</param>
    /// <param name="tessDataPath">Tesseract训练数据目录路径，默认为"./tessdata"</param>
    public OcrService(IDatabaseService databaseService, string tessDataPath = "./tessdata")
    {
        _databaseService = databaseService;
        
        // 如果使用默认路径，尝试多个可能的位置
        if (tessDataPath == "./tessdata")
        {
            // 1. 尝试当前目录下的tessdata
            var currentDirPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            if (Directory.Exists(currentDirPath))
            {
                _tessDataPath = currentDirPath;
                System.Diagnostics.Debug.WriteLine($"使用tessdata路径: {_tessDataPath}");
            }
            // 2. 尝试相对路径（开发环境）
            else if (Directory.Exists("./tessdata"))
            {
                _tessDataPath = Path.GetFullPath("./tessdata");
                System.Diagnostics.Debug.WriteLine($"使用tessdata路径: {_tessDataPath}");
            }
            else
            {
                _tessDataPath = currentDirPath; // 使用默认路径，后续会报错
                System.Diagnostics.Debug.WriteLine($"tessdata目录不存在，使用默认路径: {_tessDataPath}");
            }
        }
        else
        {
            _tessDataPath = tessDataPath;
        }
        
        // 检查训练数据文件是否存在
        CheckTessDataFiles();
    }
    
    /// <summary>
    /// 检查Tesseract训练数据文件是否存在
    /// </summary>
    private void CheckTessDataFiles()
    {
        if (!Directory.Exists(_tessDataPath))
        {
            System.Diagnostics.Debug.WriteLine($"警告: Tesseract训练数据目录不存在: {_tessDataPath}");
            System.Diagnostics.Debug.WriteLine($"请创建目录并下载训练数据文件:");
            System.Diagnostics.Debug.WriteLine($"  - chi_sim.traineddata (简体中文)");
            System.Diagnostics.Debug.WriteLine($"  - eng.traineddata (英文)");
            System.Diagnostics.Debug.WriteLine($"下载地址: https://github.com/tesseract-ocr/tessdata");
            return;
        }
        
        var chiSimFile = Path.Combine(_tessDataPath, "chi_sim.traineddata");
        var engFile = Path.Combine(_tessDataPath, "eng.traineddata");
        
        if (!File.Exists(chiSimFile))
        {
            System.Diagnostics.Debug.WriteLine($"警告: 缺少中文训练数据文件: {chiSimFile}");
        }
        
        if (!File.Exists(engFile))
        {
            System.Diagnostics.Debug.WriteLine($"警告: 缺少英文训练数据文件: {engFile}");
        }
    }
    
    /// <summary>
    /// 识别图像中指定区域的文字
    /// </summary>
    public async Task<string> RecognizeTextAsync(Avalonia.Media.Imaging.Bitmap image, RectangleF region)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 检查tessdata目录是否存在
                if (!Directory.Exists(_tessDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Tesseract训练数据目录不存在: {_tessDataPath}");
                    return string.Empty;
                }
                
                // 检查训练数据文件是否存在
                var chiSimFile = Path.Combine(_tessDataPath, "chi_sim.traineddata");
                var engFile = Path.Combine(_tessDataPath, "eng.traineddata");
                
                if (!File.Exists(chiSimFile) || !File.Exists(engFile))
                {
                    System.Diagnostics.Debug.WriteLine($"缺少训练数据文件，OCR功能不可用");
                    return string.Empty;
                }
                
                // 将Avalonia Bitmap转换为System.Drawing.Bitmap
                using var systemBitmap = ConvertToSystemBitmap(image);
                
                // 裁剪图像到指定区域
                using var croppedImage = CropImage(systemBitmap, region);
                
                // 保存裁剪后的图像用于调试
                var debugPath = Path.Combine(Path.GetTempPath(), $"ocr_debug_{DateTime.Now:yyyyMMddHHmmss}.png");
                croppedImage.Save(debugPath, System.Drawing.Imaging.ImageFormat.Png);
                System.Diagnostics.Debug.WriteLine($"裁剪图像已保存到: {debugPath}");
                
                // 将System.Drawing.Bitmap转换为Tesseract.Pix
                using var pix = Pix.LoadFromMemory(BitmapToBytes(croppedImage));
                
                // 使用Tesseract识别（中文+英文）
                using var engine = new TesseractEngine(_tessDataPath, "chi_sim+eng", EngineMode.Default);
                using var page = engine.Process(pix);
                
                var text = page.GetText().Trim();
                System.Diagnostics.Debug.WriteLine($"OCR识别结果: {text}");
                
                return text;
            }
            catch (Exception ex)
            {
                // 识别失败返回空字符串
                System.Diagnostics.Debug.WriteLine($"OCR识别失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return string.Empty;
            }
        });
    }
    
    /// <summary>
    /// 识别快递单号（左下角区域）
    /// </summary>
    public async Task<string> RecognizeTrackingNumberAsync(Avalonia.Media.Imaging.Bitmap image)
    {
        try
        {
            // 从配置中获取识别区域
            var config = await _databaseService.GetConfigAsync();
            var region = config.TrackingNumberRegion.ToRectangleF();
            
            // 验证区域有效性
            if (region.Width < 0.01f || region.Height < 0.01f)
            {
                System.Diagnostics.Debug.WriteLine("快递单号识别区域太小，请重新配置");
                return "区域无效";
            }
            
            // 快递单号使用英文识别，不设置白名单（让OCR自由识别，提取时再过滤）
            var text = await RecognizeTextWithLanguageAsync(image, region, "eng", null);
            
            if (string.IsNullOrWhiteSpace(text))
            {
                return "未识别";
            }
            
            System.Diagnostics.Debug.WriteLine($"快递单号原始识别: '{text}'");
            
            // 提取快递单号格式（会自动过滤掉中文快递名称和其他干扰字符）
            var trackingNumber = ExtractTrackingNumber(text);
            
            System.Diagnostics.Debug.WriteLine($"快递单号提取结果: '{trackingNumber}'");
            
            return string.IsNullOrWhiteSpace(trackingNumber) ? "未识别" : trackingNumber;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"识别快递单号失败: {ex.Message}");
            return "识别失败";
        }
    }
    
    /// <summary>
    /// 识别件数（快递单号附近区域）
    /// </summary>
    public async Task<string> RecognizePackageCountAsync(Avalonia.Media.Imaging.Bitmap image)
    {
        try
        {
            // 从配置中获取识别区域
            var config = await _databaseService.GetConfigAsync();
            var region = config.PackageCountRegion.ToRectangleF();
            
            // 验证区域有效性
            if (region.Width < 0.01f || region.Height < 0.01f)
            {
                System.Diagnostics.Debug.WriteLine("件数识别区域太小，请重新配置");
                return "区域无效";
            }
            
            // 件数需要中文+数字，限制字符白名单提高准确率
            var text = await RecognizeTextWithLanguageAsync(image, region, "chi_sim+eng", "0123456789件包共第（）/");
            
            if (string.IsNullOrWhiteSpace(text))
            {
                return "未识别";
            }
            
            System.Diagnostics.Debug.WriteLine($"件数原始识别: '{text}'");
            
            // 提取件数数字
            var packageCount = ExtractPackageCount(text);
            
            System.Diagnostics.Debug.WriteLine($"件数提取结果: '{packageCount}'");
            
            return string.IsNullOrWhiteSpace(packageCount) ? "未识别" : packageCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"识别件数失败: {ex.Message}");
            return "识别失败";
        }
    }
    
    /// <summary>
    /// 使用指定语言识别图像中指定区域的文字
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="region">识别区域</param>
    /// <param name="language">语言（如"eng"或"chi_sim+eng"）</param>
    /// <param name="whitelist">字符白名单（可选，限制只识别这些字符）</param>
    private async Task<string> RecognizeTextWithLanguageAsync(Avalonia.Media.Imaging.Bitmap image, RectangleF region, string language, string? whitelist = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 检查tessdata目录是否存在
                if (!Directory.Exists(_tessDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Tesseract训练数据目录不存在: {_tessDataPath}");
                    return string.Empty;
                }
                
                // 检查所需的训练数据文件是否存在
                var languages = language.Split('+');
                foreach (var lang in languages)
                {
                    var langFile = Path.Combine(_tessDataPath, $"{lang}.traineddata");
                    if (!File.Exists(langFile))
                    {
                        System.Diagnostics.Debug.WriteLine($"缺少训练数据文件: {langFile}");
                        return string.Empty;
                    }
                }
                
                // 将Avalonia Bitmap转换为System.Drawing.Bitmap
                using var systemBitmap = ConvertToSystemBitmap(image);
                
                // 裁剪图像到指定区域
                using var croppedImage = CropImage(systemBitmap, region);
                
                // 先尝试使用预处理后的图像识别
                string text = string.Empty;
                float confidence = 0;
                
                try
                {
                    // 图像预处理：提高对比度和清晰度
                    using var processedImage = PreprocessImage(croppedImage);
                    
                    // 保存预处理后的图像用于调试
                    var debugPath = Path.Combine(Path.GetTempPath(), $"ocr_debug_{language.Replace("+", "_")}_processed_{DateTime.Now:yyyyMMddHHmmss}.png");
                    processedImage.Save(debugPath, System.Drawing.Imaging.ImageFormat.Png);
                    System.Diagnostics.Debug.WriteLine($"预处理图像已保存到: {debugPath}");
                    
                    // 将System.Drawing.Bitmap转换为Tesseract.Pix
                    using var pix = Pix.LoadFromMemory(BitmapToBytes(processedImage));
                    
                    // 使用Tesseract识别
                    using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
                    
                    // 设置字符白名单（如果提供）
                    if (!string.IsNullOrEmpty(whitelist))
                    {
                        engine.SetVariable("tessedit_char_whitelist", whitelist);
                        System.Diagnostics.Debug.WriteLine($"使用字符白名单: {whitelist}");
                    }
                    
                    using var page = engine.Process(pix);
                    
                    text = page.GetText().Trim();
                    confidence = page.GetMeanConfidence();
                    
                    System.Diagnostics.Debug.WriteLine($"OCR识别结果(预处理) ({language}): '{text}' (置信度: {confidence:P})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"预处理图像识别失败: {ex.Message}，尝试使用原图");
                }
                
                // 如果预处理后识别失败或结果为空，尝试使用原图
                if (string.IsNullOrWhiteSpace(text))
                {
                    System.Diagnostics.Debug.WriteLine("预处理识别结果为空，尝试使用原图识别");
                    
                    // 保存原图用于调试
                    var debugPath = Path.Combine(Path.GetTempPath(), $"ocr_debug_{language.Replace("+", "_")}_original_{DateTime.Now:yyyyMMddHHmmss}.png");
                    croppedImage.Save(debugPath, System.Drawing.Imaging.ImageFormat.Png);
                    System.Diagnostics.Debug.WriteLine($"原始图像已保存到: {debugPath}");
                    
                    // 将System.Drawing.Bitmap转换为Tesseract.Pix
                    using var pix = Pix.LoadFromMemory(BitmapToBytes(croppedImage));
                    
                    // 使用Tesseract识别
                    using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
                    
                    // 设置字符白名单（如果提供）
                    if (!string.IsNullOrEmpty(whitelist))
                    {
                        engine.SetVariable("tessedit_char_whitelist", whitelist);
                    }
                    
                    using var page = engine.Process(pix);
                    
                    text = page.GetText().Trim();
                    confidence = page.GetMeanConfidence();
                    
                    System.Diagnostics.Debug.WriteLine($"OCR识别结果(原图) ({language}): '{text}' (置信度: {confidence:P})");
                }
                
                return text;
            }
            catch (Exception ex)
            {
                // 识别失败返回空字符串
                System.Diagnostics.Debug.WriteLine($"OCR识别失败 ({language}): {ex.Message}");
                return string.Empty;
            }
        });
    }
    
    /// <summary>
    /// 图像预处理：提高对比度和清晰度
    /// 使用Otsu自动阈值算法，更鲁棒地处理不同亮度的图像
    /// </summary>
    private System.Drawing.Bitmap PreprocessImage(System.Drawing.Bitmap source)
    {
        // 如果图像太小，不进行预处理
        if (source.Width < 50 || source.Height < 50)
        {
            System.Diagnostics.Debug.WriteLine("图像太小，跳过预处理");
            return new System.Drawing.Bitmap(source);
        }
        
        // 第一步：转换为灰度图并计算直方图
        var grayValues = new int[source.Width, source.Height];
        var histogram = new int[256];
        
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                grayValues[x, y] = gray;
                histogram[gray]++;
            }
        }
        
        int totalPixels = source.Width * source.Height;
        
        // 第二步：使用Otsu算法计算最佳阈值
        int threshold = CalculateOtsuThreshold(histogram, totalPixels);
        
        System.Diagnostics.Debug.WriteLine($"图像预处理: 使用Otsu阈值={threshold}");
        
        // 第三步：应用二值化
        var processed = new System.Drawing.Bitmap(source.Width, source.Height);
        int whiteCount = 0;
        int blackCount = 0;
        
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                int gray = grayValues[x, y];
                
                // 二值化：大于阈值为白色，否则为黑色
                int newGray = gray > threshold ? 255 : 0;
                
                if (newGray == 255) whiteCount++;
                else blackCount++;
                
                processed.SetPixel(x, y, Color.FromArgb(newGray, newGray, newGray));
            }
        }
        
        // 检查二值化结果是否合理
        float whiteRatio = (float)whiteCount / totalPixels;
        float blackRatio = (float)blackCount / totalPixels;
        
        System.Diagnostics.Debug.WriteLine($"预处理完成: 白色={whiteRatio:P}, 黑色={blackRatio:P}");
        
        // 如果图像几乎全白或全黑，说明预处理失败，返回原图
        if (whiteRatio > 0.98f || blackRatio > 0.98f)
        {
            System.Diagnostics.Debug.WriteLine($"警告: 预处理后图像过于单一，返回原图");
            processed.Dispose();
            return new System.Drawing.Bitmap(source);
        }
        
        return processed;
    }
    
    /// <summary>
    /// 使用Otsu算法计算最佳二值化阈值
    /// 该算法通过最大化类间方差来自动确定阈值，对不同亮度的图像都很鲁棒
    /// </summary>
    private int CalculateOtsuThreshold(int[] histogram, int totalPixels)
    {
        // 计算总的灰度值
        float sum = 0;
        for (int i = 0; i < 256; i++)
        {
            sum += i * histogram[i];
        }
        
        float sumB = 0;  // 背景类的灰度总和
        int wB = 0;      // 背景类的像素数
        int wF = 0;      // 前景类的像素数
        
        float maxVariance = 0;
        int threshold = 0;
        
        // 遍历所有可能的阈值
        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];  // 背景类像素数
            if (wB == 0) continue;
            
            wF = totalPixels - wB;  // 前景类像素数
            if (wF == 0) break;
            
            sumB += t * histogram[t];
            
            float mB = sumB / wB;           // 背景类平均灰度
            float mF = (sum - sumB) / wF;   // 前景类平均灰度
            
            // 计算类间方差
            float variance = wB * wF * (mB - mF) * (mB - mF);
            
            // 找到最大方差对应的阈值
            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = t;
            }
        }
        
        return threshold;
    }
    
    /// <summary>
    /// 裁剪图像到指定区域
    /// </summary>
    private System.Drawing.Bitmap CropImage(System.Drawing.Bitmap source, RectangleF region)
    {
        // 验证区域有效性
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException("裁剪区域的宽度和高度必须大于0");
        }
        
        if (region.X < 0 || region.Y < 0 || region.X >= 1 || region.Y >= 1)
        {
            throw new ArgumentException("裁剪区域的坐标必须在0-1范围内");
        }
        
        // 将相对坐标转换为绝对坐标
        int x = (int)(region.X * source.Width);
        int y = (int)(region.Y * source.Height);
        int width = (int)(region.Width * source.Width);
        int height = (int)(region.Height * source.Height);
        
        // 确保坐标在有效范围内
        x = Math.Max(0, Math.Min(x, source.Width - 1));
        y = Math.Max(0, Math.Min(y, source.Height - 1));
        
        // 确保宽度和高度至少为10像素（太小的区域无法有效识别）
        width = Math.Max(10, Math.Min(width, source.Width - x));
        height = Math.Max(10, Math.Min(height, source.Height - y));
        
        // 再次验证裁剪区域是否有效
        if (x + width > source.Width || y + height > source.Height)
        {
            // 调整到边界内
            width = source.Width - x;
            height = source.Height - y;
        }
        
        if (width < 10 || height < 10)
        {
            throw new ArgumentException($"裁剪区域太小，无法识别。最小尺寸: 10x10像素，当前: {width}x{height}像素");
        }
        
        var rect = new Rectangle(x, y, width, height);
        
        System.Diagnostics.Debug.WriteLine($"裁剪区域: X={x}, Y={y}, Width={width}, Height={height}, 源图像: {source.Width}x{source.Height}");
        
        // 裁剪图像
        var croppedBitmap = new System.Drawing.Bitmap(width, height);
        using (var graphics = Graphics.FromImage(croppedBitmap))
        {
            graphics.DrawImage(source, new Rectangle(0, 0, width, height), rect, GraphicsUnit.Pixel);
        }
        
        return croppedBitmap;
    }
    
    /// <summary>
    /// 从文本中提取快递单号
    /// 快递单号通常是连续的字母和数字组合，长度在10-20之间
    /// 会自动忽略快递公司名称（如"邮政特快专递"、"顺丰速运"等中文字符）
    /// </summary>
    private string ExtractTrackingNumber(string text)
    {
        // 先转换为大写（统一处理大小写混合的情况）
        text = text.ToUpper();
        
        // 移除所有中文字符（快递公司名称通常是中文）
        text = Regex.Replace(text, @"[\u4e00-\u9fa5]", " ");
        
        // 移除特殊符号，保留字母、数字和空格
        text = Regex.Replace(text, @"[^A-Z0-9\s]", " ");
        
        // 移除多余的空白字符
        text = Regex.Replace(text, @"\s+", " ").Trim();
        
        System.Diagnostics.Debug.WriteLine($"快递单号清理后: '{text}'");
        
        // 尝试多种匹配模式，从最严格到最宽松
        
        // 1. 优先匹配标准快递单号格式（10-20位连续字母数字）
        var match = Regex.Match(text, @"\b[A-Z0-9]{10,20}\b");
        if (match.Success)
        {
            System.Diagnostics.Debug.WriteLine($"匹配模式1: {match.Value}");
            return match.Value;
        }
        
        // 2. 匹配带空格的单号（去除空格后长度在10-20之间）
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Length >= 10 && part.Length <= 20 && Regex.IsMatch(part, @"^[A-Z0-9]+$"))
            {
                System.Diagnostics.Debug.WriteLine($"匹配模式2: {part}");
                return part;
            }
        }
        
        // 3. 尝试组合相邻的部分
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var combined = parts[i] + parts[i + 1];
            if (combined.Length >= 10 && combined.Length <= 20 && Regex.IsMatch(combined, @"^[A-Z0-9]+$"))
            {
                System.Diagnostics.Debug.WriteLine($"匹配模式3: {combined}");
                return combined;
            }
        }
        
        // 4. 如果都没匹配到，返回最长的字母数字组合（至少8位）
        var longest = parts
            .Where(p => Regex.IsMatch(p, @"^[A-Z0-9]+$") && p.Length >= 8)
            .OrderByDescending(p => p.Length)
            .FirstOrDefault();
        
        if (longest != null)
        {
            System.Diagnostics.Debug.WriteLine($"匹配模式4: {longest}");
        }
        
        return longest ?? string.Empty;
    }
    
    /// <summary>
    /// 从文本中提取件数
    /// 支持格式：
    /// - "8件"
    /// - "第1包（共1包）"
    /// - "1/1"
    /// </summary>
    private string ExtractPackageCount(string text)
    {
        // 匹配 "数字件" 格式
        var match1 = Regex.Match(text, @"(\d+)\s*件");
        if (match1.Success)
        {
            return match1.Groups[1].Value + "件";
        }
        
        // 匹配 "第X包（共Y包）" 格式
        var match2 = Regex.Match(text, @"共\s*(\d+)\s*包");
        if (match2.Success)
        {
            return match2.Groups[1].Value + "件";
        }
        
        // 匹配 "X/Y" 格式
        var match3 = Regex.Match(text, @"(\d+)\s*/\s*(\d+)");
        if (match3.Success)
        {
            return match3.Groups[2].Value + "件";
        }
        
        // 如果都不匹配，尝试提取第一个数字
        var match4 = Regex.Match(text, @"\d+");
        if (match4.Success)
        {
            return match4.Value + "件";
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// 将Avalonia Bitmap转换为System.Drawing.Bitmap
    /// </summary>
    private System.Drawing.Bitmap ConvertToSystemBitmap(Avalonia.Media.Imaging.Bitmap avaloniaBitmap)
    {
        using var memoryStream = new MemoryStream();
        
        // 将Avalonia Bitmap保存到内存流
        avaloniaBitmap.Save(memoryStream);
        memoryStream.Position = 0;
        
        // 从内存流创建System.Drawing.Bitmap
        return new System.Drawing.Bitmap(memoryStream);
    }
    
    /// <summary>
    /// 将System.Drawing.Bitmap转换为字节数组
    /// </summary>
    private byte[] BitmapToBytes(System.Drawing.Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        return memoryStream.ToArray();
    }
}
