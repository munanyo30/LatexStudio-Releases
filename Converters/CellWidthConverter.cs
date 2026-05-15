using System.Globalization;
using System.Windows.Data;

namespace LatexStudio.Converters;

public class CellWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2)
        {
            // Check for DependencyProperty.UnsetValue (MS.Internal.NamedObject)
            if (values[0] == System.Windows.DependencyProperty.UnsetValue || 
                values[1] == System.Windows.DependencyProperty.UnsetValue)
                return 72.0;

            double baseWidth = System.Convert.ToDouble(values[0], culture);
            int span = System.Convert.ToInt32(values[1], culture);
            
            // Calculate size: BaseSize * Span (no extra gap since Margin is now 0)
            return baseWidth * span;
        }
        return 72.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
