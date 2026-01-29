using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 标签选择对话框ViewModel
/// </summary>
public partial class LabelSelectionViewModel : ViewModelBase
{
    /// <summary>
    /// 条码分组列表
    /// </summary>
    public ObservableCollection<BarcodeGroupItemViewModel> BarcodeGroups { get; }
    
    /// <summary>
    /// 选中的分组
    /// </summary>
    [ObservableProperty]
    private BarcodeGroupItemViewModel? _selectedGroup;
    
    /// <summary>
    /// 对话框结果
    /// </summary>
    public BarcodeGroup? Result { get; private set; }
    
    /// <summary>
    /// 对话框关闭事件
    /// </summary>
    public event EventHandler? CloseRequested;
    
    public LabelSelectionViewModel(List<BarcodeGroup> groups)
    {
        BarcodeGroups = new ObservableCollection<BarcodeGroupItemViewModel>(
            groups.Select(g => new BarcodeGroupItemViewModel(g, this))
        );
    }
    
    /// <summary>
    /// 选择分组
    /// </summary>
    public void SelectGroup(BarcodeGroupItemViewModel group)
    {
        // 取消之前的选择
        if (SelectedGroup != null)
        {
            SelectedGroup.IsSelected = false;
        }
        
        // 选中新的分组
        SelectedGroup = group;
        group.IsSelected = true;
    }
    
    /// <summary>
    /// 确定命令
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        Result = SelectedGroup?.Group;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 取消命令
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
