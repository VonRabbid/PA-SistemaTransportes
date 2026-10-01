using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SistemaTransportes.Converters;

/// <summary>
/// Muestra u oculta un elemento segun el valor sea null o vacio.
/// </summary>
public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool esVacio = value == null || string.IsNullOrWhiteSpace(value.ToString());

        if (Invert)
        {
            esVacio = !esVacio;
        }

        return esVacio ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
