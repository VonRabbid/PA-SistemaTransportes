namespace SistemaTransportes.Domain.Entities;

public class EstadoAsientoViaje
{
    public int EstadoAsientoID { get; set; }
    public int ViajeID { get; set; }
    public int NroAsiento { get; set; }
    public string Estado { get; set; } = "Libre";
    public byte[]? RowVersion { get; set; }
}
