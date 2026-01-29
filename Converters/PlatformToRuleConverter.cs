using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Converters;

/// <summary>
/// 平台到计算规则的转换器
/// </summary>
public class PlatformToRuleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Platform platform)
        {
            return platform switch
            {
                Platform.TEMU => "条码页数 - 主单页数 + 1",
                Platform.SHEIN => "条码页数",
                _ => "未知规则"
            };
        }
        
        return "未知规则";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
