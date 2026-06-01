using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PrintToolAvalonia.Models;

public partial class TemplateLabelQueueItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string BarcodePdfPath { get; set; } = string.Empty;

    public BarcodeGroup BarcodeGroup { get; set; } = new();

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private int _reprintCount;

    [ObservableProperty]
    private bool _isBasePrintCompleted;

    /// <summary>
    /// 商品名称（中文）- 打单时人工查看
    /// </summary>
    [ObservableProperty]
    private string _productNameChinese = string.Empty;

    /// <summary>
    /// 商品名称（英文）- 实际打印到标签上
    /// </summary>
    [ObservableProperty]
    private string _productNameEnglish = string.Empty;

    /// <summary>
    /// 选中的商品名称项（用于下拉选择自动填充）
    /// </summary>
    [ObservableProperty]
    private ProductNameItem? _selectedProductNameItem;

    partial void OnSelectedProductNameItemChanged(ProductNameItem? value)
    {
        if (value != null)
        {
            ProductNameChinese = value.ChineseName;
            ProductNameEnglish = value.EnglishName;
        }
    }

    public int LabelCount => BarcodeGroup.BarcodeCount;

    public string PrintStatusDisplay => IsBasePrintCompleted ? "已首打" : "待首打";

    public string GroupDisplay => $"第{BarcodeGroup.StartPage}-{BarcodeGroup.EndPage}页";

    public string SourceFileName => Path.GetFileName(BarcodePdfPath);

    /// <summary>
    /// 商品名称显示（中文 - 英文）
    /// </summary>
    public string ProductNameDisplay => string.IsNullOrWhiteSpace(ProductNameChinese) && string.IsNullOrWhiteSpace(ProductNameEnglish)
        ? "未设置商品名称"
        : $"{ProductNameChinese} / {ProductNameEnglish}";

    partial void OnIsBasePrintCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(PrintStatusDisplay));
    }

    partial void OnProductNameChineseChanged(string value)
    {
        OnPropertyChanged(nameof(ProductNameDisplay));
    }

    partial void OnProductNameEnglishChanged(string value)
    {
        OnPropertyChanged(nameof(ProductNameDisplay));
    }
}
