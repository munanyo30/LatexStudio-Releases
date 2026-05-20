using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LatexStudio.Core.Converters;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;

        string? currentState = value.ToString();
        string? targetState = parameter.ToString();

        if (currentState == null || targetState == null) return Visibility.Collapsed;

        return currentState.Equals(targetState, StringComparison.OrdinalIgnoreCase) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
