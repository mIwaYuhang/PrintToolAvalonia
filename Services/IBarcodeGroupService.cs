using PrintToolAvalonia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 条码分组服务接口
/// 管理条码分组逻辑和打印状态
/// </summary>
public interface IBarcodeGroupService
{
    /// <summary>
    /// 根据分隔符页码创建条码分组
    /// </summary>
    /// <param name="pdfFilePath">条码PDF文件路径</param>
    /// <param name="separatorPages">分隔符页码列表</param>
    /// <returns>条码分组列表</returns>
    Task<List<BarcodeGroup>> CreateGroupsAsync(string pdfFilePath, List<int> separatorPages);
    
    /// <summary>
    /// 标记分组为已打印
    /// </summary>
    /// <param name="groupId">分组ID</param>
    void MarkAsPrinted(string groupId);
    
    /// <summary>
    /// 检查分组是否已打印
    /// </summary>
    /// <param name="groupId">分组ID</param>
    /// <returns>是否已打印</returns>
    bool IsPrinted(string groupId);
    
    /// <summary>
    /// 清除所有打印状态（用于新会话）
    /// </summary>
    void ClearPrintStatus();
}
