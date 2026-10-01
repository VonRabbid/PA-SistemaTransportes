using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class ViajeItemViewModel : ViewModelBase
{
    public int ViajeID { get; set; }
    public string Origen { get; set; } = "Lima";
    public string Destino { get; set; } = "Huancayo";
    public string BusPlaca { get; set; } = "ABC-123";
    public string PlacaBus => BusPlaca;
    public string TipoServicio { get; set; } = "Servicio Directo";
    public string Categoria { get; set; } = "VIP";
    public string HoraSalidaTexto { get; set; } = "08:00 AM";
    public string HoraLlegadaTexto { get; set; } = "03:30 PM";
    public string Duracion { get; set; } = "07h 30m";
    public DateTime FechaHoraSalida { get; set; } = DateTime.Today.AddHours(8);
    public DateTime FechaHoraLlegada { get; set; } = DateTime.Today.AddHours(15).AddMinutes(30);
    public decimal PrecioBase { get; set; } = 65.00m;

    public bool EsIdaYVuelta { get; set; }
    public string EtiquetaTarifa => EsIdaYVuelta ? "Por Pasajero (Ida y Vuelta)" : "Por Pasajero";

    public bool SalidaVencida => FechaHoraSalida < DateTime.Now;
    public bool SalidaDisponible => !SalidaVencida;

    public string TextoBotonAccion
    {
        get
        {
            if (SalidaVencida)
                return "Horario no disponible";

            return "Comprar";
        }
    }

    public string RutaTexto => $"{Origen} ➔ {Destino}";
    public string SalidaCompletaTexto => $"Salida: {HoraSalidaTexto} ({RutaTexto})";
}
