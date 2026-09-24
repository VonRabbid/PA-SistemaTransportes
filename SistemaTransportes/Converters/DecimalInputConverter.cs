using System.Globalization;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Intercambio bidireccional entre un decimal y su representacion de texto
/// en los campos de captura de importes y pesos.
/// </summary>
public class DecimalInputConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal decimalValue => decimalValue.ToString("0.##", CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("0.##", CultureInfo.InvariantCulture),
            _ => "0"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? text = value?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(text) || text is "." or ",")
        {
            return 0m;
        }

        // Acepta coma o punto como separador decimal.
        text = text.Replace(',', '.');

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultado))
        {
            return resultado >= 0 ? resultado : 0m;
        }

        return 0m;
    }
}
