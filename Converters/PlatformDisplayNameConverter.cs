using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Converters;

/// <summary>
/// 平台枚举到界面显示名称的转换器
/// </summary>
public class PlatformDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Platform platform)
        {
            return GetDisplayName(platform);
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取平台的中文显示名称
    /// </summary>
    public static string GetDisplayName(Platform platform)
    {
        return platform switch
        {
            Platform.TEMU => "TEMU",
            Platform.SHEIN => "SHEIN",
            Platform.SHEIN_SPECIAL => "冷希音特供款",
            _ => platform.ToString()
        };
    }
}
