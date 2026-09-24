using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SistemaTransportes.Converters;

/// <summary>
/// Resuelve el brush de un asiento segun su estado, con recurso en el
/// diccionario de la aplicacion y un fallback de color directo.
/// </summary>
public class EstadoAsientoToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string estado)
        {
            return new SolidColorBrush(Colors.LightGray);
        }

        string estadoNormalizado = estado.Trim().ToLowerInvariant();

        string clave = estadoNormalizado switch
        {
            "libre" => "AsientoLibreBrush",
            "ocupado" or "reservado" => "AsientoOcupadoBrush",
            "seleccionado" => "AsientoSeleccionadoBrush",
            _ => "TextSecondaryBrush"
        };

        // Se usa el alias para evitar la colision con el namespace Transportes.Application.
        var app = System.Windows.Application.Current;
        if (app?.Resources.Contains(clave) == true)
        {
            return (Brush)app.Resources[clave];
        }

        // Fallback cuando la clave no esta registrada en el diccionario de recursos.
        return estadoNormalizado switch
        {
            "libre" => CrearBrush("#2ECC71"),
            "ocupado" => CrearBrush("#E74C3C"),
            "seleccionado" => CrearBrush("#2B6CB0"),
            _ => CrearBrush("#BDC3C7")
        };
    }

    private static SolidColorBrush CrearBrush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
