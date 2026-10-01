namespace SistemaTransportes.Application.DTOs;

public class RegistroVentaRequestDTO
{
    public int ViajeId { get; set; }
    public int CajaTurnoId { get; set; }
    public List<PasajeroDTO> Pasajeros { get; set; } = new();
    public EncomiendaDTO? Encomienda { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string? NumeroOperacion { get; set; }
    public decimal MontoTotal { get; set; }
    public bool EsSoloEncomienda => Pasajeros.Count == 0 && Encomienda != null;
}
