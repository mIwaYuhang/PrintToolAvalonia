using System.Collections.Generic;
using System.Threading.Tasks;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 数据库服务接口
/// </summary>
public interface IDatabaseService
{
    // ========== 配置管理 ==========
    
    /// <summary>
    /// 获取应用配置
    /// </summary>
    Task<AppConfig> GetConfigAsync();
    
    /// <summary>
    /// 保存应用配置
    /// </summary>
    Task SaveConfigAsync(AppConfig config);
    
    // ========== 环保码管理 ==========
    
    /// <summary>
    /// 获取所有环保码
    /// </summary>
    Task<List<EcoCodeItem>> GetAllEcoCodesAsync();
    
    /// <summary>
    /// 根据 ID 获取环保码
    /// </summary>
    Task<EcoCodeItem?> GetEcoCodeByIdAsync(string id);
    
    /// <summary>
    /// 添加环保码
    /// </summary>
    Task<EcoCodeItem> AddEcoCodeAsync(EcoCodeItem item);
    
    /// <summary>
    /// 更新环保码
    /// </summary>
    Task UpdateEcoCodeAsync(string id, EcoCodeItem item);
    
    /// <summary>
    /// 删除环保码
    /// </summary>
    Task DeleteEcoCodeAsync(string id);
    
    // ========== 打印历史管理 ==========
    
    /// <summary>
    /// 获取所有打印记录
    /// </summary>
    /// <param name="filter">日期过滤器（可选）</param>
    Task<List<PrintRecord>> GetAllRecordsAsync(DateFilter? filter = null);
    
    /// <summary>
    /// 添加打印记录
    /// </summary>
    Task<PrintRecord> AddRecordAsync(PrintRecord record);
    
    /// <summary>
    /// 删除打印记录
    /// </summary>
    Task DeleteRecordAsync(string id);
    
    /// <summary>
    /// 清空所有打印记录
    /// </summary>
    Task ClearAllRecordsAsync();
}
