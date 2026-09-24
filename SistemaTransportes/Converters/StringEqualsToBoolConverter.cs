using System.Globalization;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Compara el valor enlazado con el parametro del converter.
/// Utilizado por los radio buttons de seleccion por opcion.
/// </summary>
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        return string.Equals(
            value.ToString()?.Trim(),
            parameter.ToString()?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag && flag && parameter != null)
        {
            return parameter.ToString()!;
        }

        return Binding.DoNothing;
    }
}
