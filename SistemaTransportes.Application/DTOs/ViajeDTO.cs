namespace SistemaTransportes.Application.DTOs;

public class ViajeDTO
{
    public int ViajeID { get; set; }
    public int BusID { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string TipoServicio { get; set; } = "Directo";
    public DateTime FechaSalida { get; set; }
    public DateTime? FechaHoraLlegada { get; set; }
    public string Categoria { get; set; } = "Clásico";
    public string? DuracionEstimada { get; set; }
    public decimal PrecioBase { get; set; }
    public string PlacaBus { get; set; } = string.Empty;
    public int CapacidadBus { get; set; }
    public int AsientosLibres { get; set; }
}
