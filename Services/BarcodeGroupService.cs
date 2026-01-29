using PrintToolAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 条码分组服务实现
/// 根据分隔符创建分组并管理打印状态
/// </summary>
public class BarcodeGroupService : IBarcodeGroupService
{
    private readonly IPdfRenderService _pdfRenderService;
    private readonly Dictionary<string, BarcodeGroup> _groups = new();
    
    public BarcodeGroupService(IPdfRenderService pdfRenderService)
    {
        _pdfRenderService = pdfRenderService;
    }
    
    /// <summary>
    /// 根据分隔符页码创建条码分组
    /// 分组逻辑：分隔符前的页面归为一组，排除分隔符本身
    /// </summary>
    public async Task<List<BarcodeGroup>> CreateGroupsAsync(string pdfFilePath, List<int> separatorPages)
    {
        var groups = new List<BarcodeGroup>();
        
        try
        {
            var pageCount = await _pdfRenderService.GetPageCountAsync(pdfFilePath);
            
            // 排序分隔符页码
            var sortedSeparators = separatorPages.OrderBy(p => p).ToList();
            
            int currentStart = 1;
            
            foreach (var separatorPage in sortedSeparators)
            {
                // 如果分隔符前面有页面，创建一个分组
                // 分组包含从currentStart到separatorPage-1的页面
                if (separatorPage > currentStart)
                {
                    var group = new BarcodeGroup
                    {
                        StartPage = currentStart,
                        EndPage = separatorPage - 1
                    };
                    
                    // 渲染最后一页作为预览（使用100 DPI以节省内存）
                    group.PreviewImage = await _pdfRenderService.RenderPageAsync(
                        pdfFilePath, 
                        group.EndPage, 
                        100
                    );
                    
                    groups.Add(group);
                    _groups[group.Id] = group;
                }
                
                // 下一组从分隔符后一页开始（排除分隔符本身）
                currentStart = separatorPage + 1;
            }
            
            // 处理最后一组（如果有剩余页面）
            if (currentStart <= pageCount)
            {
                var group = new BarcodeGroup
                {
                    StartPage = currentStart,
                    EndPage = pageCount
                };
                
                // 渲染最后一页作为预览
                group.PreviewImage = await _pdfRenderService.RenderPageAsync(
                    pdfFilePath, 
                    group.EndPage, 
                    100
                );
                
                groups.Add(group);
                _groups[group.Id] = group;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建条码分组失败: {ex.Message}");
        }
        
        return groups;
    }
    
    /// <summary>
    /// 标记分组为已打印
    /// </summary>
    public void MarkAsPrinted(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.IsPrinted = true;
            group.PrintedAt = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 检查分组是否已打印
    /// </summary>
    public bool IsPrinted(string groupId)
    {
        return _groups.TryGetValue(groupId, out var group) && group.IsPrinted;
    }
    
    /// <summary>
    /// 清除所有打印状态
    /// </summary>
    public void ClearPrintStatus()
    {
        foreach (var group in _groups.Values)
        {
            group.IsPrinted = false;
            group.PrintedAt = null;
        }
    }
}
