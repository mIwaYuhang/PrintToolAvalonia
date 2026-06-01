using System;

namespace PrintToolAvalonia.Models;

/// <summary>
/// 商品名称项（中英文对照）
/// 用于希音平台环保码标签打印时，每个条码对应一个商品名称
/// </summary>
public class ProductNameItem
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 商品名称（中文）- 用于打单时人工查看
    /// </summary>
    public string ChineseName { get; set; } = string.Empty;

    /// <summary>
    /// 商品名称（英文）- 用于实际打印到标签上
    /// </summary>
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 显示文本（中文 - 英文）
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(ChineseName)
        ? EnglishName
        : string.IsNullOrWhiteSpace(EnglishName)
            ? ChineseName
            : $"{ChineseName} - {EnglishName}";
}
