namespace PrintToolAvalonia.Services;

/// <summary>
/// 打印结果
/// </summary>
public class PrintResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? Error { get; set; }
}
