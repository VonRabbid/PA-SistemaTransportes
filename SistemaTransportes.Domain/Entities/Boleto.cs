namespace SistemaTransportes.Domain.Entities;

public class Boleto
{
    public int BoletoID { get; set; }
    public int ViajeID { get; set; }
    public int NroAsiento { get; set; }
    public string DniPasajero { get; set; } = string.Empty;
    public string NombrePasajero { get; set; } = string.Empty;
    public decimal PrecioFinal { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public int CajaTurnoID { get; set; }
    public string? MetodoPago { get; set; } = "Efectivo";
    public string? NumeroOperacion { get; set; }
}
