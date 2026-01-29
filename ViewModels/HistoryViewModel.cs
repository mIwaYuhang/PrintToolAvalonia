using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 历史对话框 ViewModel
/// </summary>
public class HistoryViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    // ========== 历史记录 ==========
    
    /// <summary>
    /// 打印记录列表
    /// </summary>
    public ObservableCollection<PrintRecord> Records { get; } = new();

    // ========== 日期过滤 ==========
    
    private DateTime? _startDate;
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }
    
    private DateTime? _endDate;
    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // ========== 命令 ==========
    
    public ICommand FilterCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand CloseCommand { get; }

    // ========== 事件 ==========
    
    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event EventHandler? RequestClose;

    public HistoryViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;

        // 初始化命令
        FilterCommand = new AsyncRelayCommand(FilterAsync);
        ClearAllCommand = new AsyncRelayCommand(ClearAllAsync);
        CloseCommand = new RelayCommand(Close);

        // 加载数据
        _ = LoadRecordsAsync();
    }

    /// <summary>
    /// 加载历史记录
    /// </summary>
    public async Task LoadRecordsAsync(DateFilter? filter = null)
    {
        try
        {
            var records = await _databaseService.GetAllRecordsAsync(filter);
            
            Records.Clear();
            foreach (var record in records)
            {
                Records.Add(record);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 按日期范围过滤
    /// </summary>
    private async Task FilterAsync()
    {
        try
        {
            var filter = new DateFilter
            {
                StartDate = StartDate,
                EndDate = EndDate
            };

            await LoadRecordsAsync(filter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"过滤历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清空所有历史记录
    /// </summary>
    private async Task ClearAllAsync()
    {
        try
        {
            // TODO: 添加确认对话框
            await _databaseService.ClearAllRecordsAsync();
            Records.Clear();
            
            Console.WriteLine("历史记录已清空");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清空历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭对话框
    /// </summary>
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
