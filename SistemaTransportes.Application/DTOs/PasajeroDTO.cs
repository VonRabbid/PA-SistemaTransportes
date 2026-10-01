namespace SistemaTransportes.Application.DTOs;

public class PasajeroDTO
{
    public int NroAsiento { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}
