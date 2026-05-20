using System.Globalization;
using System.Windows.Data;

namespace LatexStudio.Core.Converters;

public class IndexToPositionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2)
        {
            if (values[0] == System.Windows.DependencyProperty.UnsetValue || 
                values[1] == System.Windows.DependencyProperty.UnsetValue)
                return 0.0;

            int index = System.Convert.ToInt32(values[0], culture);
            double size = System.Convert.ToDouble(values[1], culture);
            
            return (double)index * size;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
