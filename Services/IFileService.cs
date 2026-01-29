using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 文件服务接口
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 验证 PDF 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>验证结果</returns>
    Task<PdfValidationResult> ValidatePdfAsync(string filePath);
    
    /// <summary>
    /// 复制文件到应用数据目录
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="destName">目标文件名</param>
    /// <returns>目标文件完整路径</returns>
    Task<string> CopyToAppDataAsync(string sourcePath, string destName);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    Task DeleteFileAsync(string filePath);
    
    /// <summary>
    /// 打开文件选择对话框
    /// </summary>
    /// <param name="filter">文件过滤器（例如："PDF Files|*.pdf"）</param>
    /// <returns>选择的文件路径数组</returns>
    Task<string[]> OpenFileDialogAsync(string filter);
    
    /// <summary>
    /// 打开文件选择对话框
    /// </summary>
    /// <param name="filter">文件过滤器（例如："PDF Files|*.pdf"）</param>
    /// <param name="owner">父窗口</param>
    /// <returns>选择的文件路径数组</returns>
    Task<string[]> OpenFileDialogAsync(string filter, Avalonia.Controls.Window? owner);
}
