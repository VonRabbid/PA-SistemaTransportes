using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Convierte un booleano en Visibility, con soporte de inversion.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool resultado = value is bool flag && flag;
        if (Invert)
        {
            resultado = !resultado;
        }

        return resultado ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool resultado = visibility == Visibility.Visible;
            return Invert ? !resultado : resultado;
        }

        return false;
    }
}
