using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BloodyMaryLauncher.ViewModels;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    private static readonly Color ActiveColor = Color.Parse("#8B1A1A");
    private static readonly Color InactiveColor = Color.Parse("#1E1714");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
            return isActive ? ActiveColor : InactiveColor;
        return InactiveColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
