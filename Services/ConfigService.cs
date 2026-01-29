using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigService : IConfigService
{
    private const string ConfigFileName = "config.json";
    private readonly string _appDataPath;
    private readonly string _configFilePath;

    public ConfigService()
    {
        // 获取应用数据目录：%LOCALAPPDATA%\PrintToolAvalonia
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintToolAvalonia"
        );
        
        _configFilePath = Path.Combine(_appDataPath, ConfigFileName);
        
        // 确保应用数据目录存在
        if (!Directory.Exists(_appDataPath))
        {
            Directory.CreateDirectory(_appDataPath);
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                // 配置文件不存在，创建默认配置并保存
                var defaultConfig = new AppConfig();
                await SaveAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            // 配置文件损坏，尝试从备份恢复
            Console.WriteLine($"加载配置失败: {ex.Message}");
            
            var backupPath = _configFilePath + ".backup";
            if (File.Exists(backupPath))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(backupPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(backupJson);
                    if (config != null)
                    {
                        Console.WriteLine("从备份恢复配置成功");
                        // 恢复主配置文件
                        await SaveAsync(config);
                        return config;
                    }
                }
                catch
                {
                    Console.WriteLine("备份文件也已损坏");
                }
            }
            
            // 返回默认配置
            return new AppConfig();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveAsync(AppConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,  // 格式化输出
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 支持中文
            };

            var json = JsonSerializer.Serialize(config, options);
            
            // 如果配置文件存在，先备份
            if (File.Exists(_configFilePath))
            {
                var backupPath = _configFilePath + ".backup";
                File.Copy(_configFilePath, backupPath, overwrite: true);
            }
            
            // 保存新配置
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取应用数据目录路径
    /// </summary>
    public string GetAppDataPath()
    {
        return _appDataPath;
    }
}
