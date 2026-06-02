namespace PrintToolAvalonia.Models;

/// <summary>
/// 电商平台枚举
/// </summary>
public enum Platform
{
    /// <summary>
    /// TEMU 平台
    /// </summary>
    TEMU,
    
    /// <summary>
    /// SHEIN 平台
    /// </summary>
    SHEIN,

    /// <summary>
    /// 冷希音特供款
    /// 基于 SHEIN 的特殊变体：不合成条码，直接打印 60x80 环保标签，
    /// 仍可选择条码分组与商品名称，但不会把条码合并进标签。
    /// </summary>
    SHEIN_SPECIAL
}
