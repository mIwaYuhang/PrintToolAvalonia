using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 条码分组项ViewModel
/// 用于在标签选择对话框中显示单个分组
/// </summary>
public partial class BarcodeGroupItemViewModel : ViewModelBase
{
    private readonly LabelSelectionViewModel _parent;
    
    /// <summary>
    /// 条码分组数据
    /// </summary>
    public BarcodeGroup Group { get; }
    
    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
    
    /// <summary>
    /// 条码数量文本（"N个条码"格式）
    /// </summary>
    public string BarcodeCountText => $"{Group.BarcodeCount}个条码";
    
    /// <summary>
    /// 分组信息（"第X-Y页"格式）
    /// </summary>
    public string GroupInfo => $"第{Group.StartPage}-{Group.EndPage}页";
    
    /// <summary>
    /// 预览图
    /// </summary>
    public Bitmap? PreviewImage => Group.PreviewImage;
    
    /// <summary>
    /// 是否已打印
    /// </summary>
    public bool IsPrinted => Group.IsPrinted;
    
    public BarcodeGroupItemViewModel(BarcodeGroup group, LabelSelectionViewModel parent)
    {
        Group = group;
        _parent = parent;
    }
    
    /// <summary>
    /// 选择命令
    /// </summary>
    [RelayCommand]
    private void Select()
    {
        _parent.SelectGroup(this);
    }
}
