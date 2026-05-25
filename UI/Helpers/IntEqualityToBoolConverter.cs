using System.Globalization;
using System.Windows.Data;

namespace UI.Helpers;

public sealed class IntEqualityToBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [var left, var right, ..])
            return false;

        if (left is not int heatId)
            return false;

        return right is int selectedHeatId && heatId == selectedHeatId;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
