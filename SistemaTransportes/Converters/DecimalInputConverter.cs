using System;
using System.Globalization;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Intercambio bidireccional entre un decimal (o decimal anulable) y su representacion de texto
/// en los campos de captura de importes y pesos.
/// </summary>
public class DecimalInputConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            decimal decimalValue => decimalValue == 0m && parameter?.ToString() == "AllowEmpty"
                ? string.Empty
                : decimalValue.ToString("0.##", CultureInfo.InvariantCulture),
            double doubleValue => doubleValue == 0.0 && parameter?.ToString() == "AllowEmpty"
                ? string.Empty
                : doubleValue.ToString("0.##", CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? text = value?.ToString()?.Trim();

        bool esNullable = targetType == typeof(decimal?) ||
                          Nullable.GetUnderlyingType(targetType) != null ||
                          targetType == typeof(object);

        if (string.IsNullOrWhiteSpace(text))
        {
            return esNullable ? null : 0m;
        }

        if (text is "." or ",")
        {
            return esNullable ? null : 0m;
        }

        // Acepta coma o punto como separador decimal.
        text = text.Replace(',', '.');

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultado))
        {
            return resultado >= 0 ? resultado : (esNullable ? null : 0m);
        }

        return esNullable ? null : 0m;
    }
}
