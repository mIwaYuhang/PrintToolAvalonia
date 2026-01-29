using System.Threading.Tasks;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 加载配置
    /// </summary>
    /// <returns>应用配置</returns>
    Task<AppConfig> LoadAsync();
    
    /// <summary>
    /// 保存配置
    /// </summary>
    /// <param name="config">应用配置</param>
    Task SaveAsync(AppConfig config);
    
    /// <summary>
    /// 获取应用数据目录路径
    /// </summary>
    /// <returns>应用数据目录完整路径</returns>
    string GetAppDataPath();
}
