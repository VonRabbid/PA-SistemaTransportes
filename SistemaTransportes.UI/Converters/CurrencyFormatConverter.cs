using System.Globalization;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Formatea importes con el prefijo de moneda local.
/// </summary>
public class CurrencyFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal decimalValue => $"S/. {decimalValue:N2}",
            double doubleValue => $"S/. {doubleValue:N2}",
            _ => "S/. 0.00"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
