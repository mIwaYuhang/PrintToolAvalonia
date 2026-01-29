using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 数据库服务实现（使用 LiteDB）
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public DatabaseService(IConfigService configService)
    {
        // 数据库文件路径
        _dbPath = Path.Combine(configService.GetAppDataPath(), "printtool.db");
        
        // 创建或打开数据库
        _db = new LiteDatabase(_dbPath);
    }

    // ========== 配置管理 ==========

    public Task<AppConfig> GetConfigAsync()
    {
        var collection = _db.GetCollection<AppConfig>("config");
        var config = collection.FindById("config") ?? new AppConfig();
        return Task.FromResult(config);
    }

    public Task SaveConfigAsync(AppConfig config)
    {
        var collection = _db.GetCollection<AppConfig>("config");
        config.Id = "config";  // 确保 ID 固定
        collection.Upsert(config);
        return Task.CompletedTask;
    }

    // ========== 环保码管理 ==========

    public Task<List<EcoCodeItem>> GetAllEcoCodesAsync()
    {
        var collection = _db.GetCollection<EcoCodeItem>("ecocodes");
        var items = collection.FindAll().ToList();
        return Task.FromResult(items);
    }

    public Task<EcoCodeItem?> GetEcoCodeByIdAsync(string id)
    {
        var collection = _db.GetCollection<EcoCodeItem>("ecocodes");
        var item = collection.FindById(id);
        return Task.FromResult(item);
    }

    public Task<EcoCodeItem> AddEcoCodeAsync(EcoCodeItem item)
    {
        var collection = _db.GetCollection<EcoCodeItem>("ecocodes");
        
        // 确保有唯一 ID
        if (string.IsNullOrEmpty(item.Id))
        {
            item.Id = Guid.NewGuid().ToString();
        }
        
        collection.Insert(item);
        return Task.FromResult(item);
    }

    public Task UpdateEcoCodeAsync(string id, EcoCodeItem item)
    {
        var collection = _db.GetCollection<EcoCodeItem>("ecocodes");
        item.Id = id;  // 确保 ID 不变
        collection.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteEcoCodeAsync(string id)
    {
        var collection = _db.GetCollection<EcoCodeItem>("ecocodes");
        collection.Delete(id);
        return Task.CompletedTask;
    }

    // ========== 打印历史管理 ==========

    public Task<List<PrintRecord>> GetAllRecordsAsync(DateFilter? filter = null)
    {
        var collection = _db.GetCollection<PrintRecord>("records");
        
        IEnumerable<PrintRecord> query = collection.FindAll();
        
        // 应用日期过滤
        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.Timestamp >= filter.StartDate.Value);
            }
            
            if (filter.EndDate.HasValue)
            {
                // 包含结束日期的整天
                var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.Timestamp <= endOfDay);
            }
        }
        
        // 按时间降序排序（最新的在前）
        var records = query.OrderByDescending(r => r.Timestamp).ToList();
        
        return Task.FromResult(records);
    }

    public Task<PrintRecord> AddRecordAsync(PrintRecord record)
    {
        var collection = _db.GetCollection<PrintRecord>("records");
        
        // 确保有唯一 ID
        if (string.IsNullOrEmpty(record.Id))
        {
            record.Id = Guid.NewGuid().ToString();
        }
        
        collection.Insert(record);
        return Task.FromResult(record);
    }

    public Task DeleteRecordAsync(string id)
    {
        var collection = _db.GetCollection<PrintRecord>("records");
        collection.Delete(id);
        return Task.CompletedTask;
    }

    public Task ClearAllRecordsAsync()
    {
        var collection = _db.GetCollection<PrintRecord>("records");
        collection.DeleteAll();
        return Task.CompletedTask;
    }

    // ========== 资源释放 ==========

    public void Dispose()
    {
        _db?.Dispose();
    }
}
