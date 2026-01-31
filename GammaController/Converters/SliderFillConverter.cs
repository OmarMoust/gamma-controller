using System.Globalization;
using System.Windows.Data;

namespace GammaController.Converters;

public class SliderFillConverter : IMultiValueConverter
{
    public static readonly SliderFillConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4 ||
            values[0] is not double value ||
            values[1] is not double minimum ||
            values[2] is not double maximum ||
            values[3] is not double totalWidth)
        {
            return 0.0;
        }

        if (maximum <= minimum || totalWidth <= 0)
            return 0.0;

        double percent = (value - minimum) / (maximum - minimum);
        return Math.Max(0, percent * totalWidth);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

