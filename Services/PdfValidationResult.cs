namespace PrintToolAvalonia.Services;

/// <summary>
/// PDF 验证结果
/// </summary>
public class PdfValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// PDF 页数
    /// </summary>
    public int PageCount { get; set; }
    
    /// <summary>
    /// 错误信息（如果无效）
    /// </summary>
    public string? Error { get; set; }
}
