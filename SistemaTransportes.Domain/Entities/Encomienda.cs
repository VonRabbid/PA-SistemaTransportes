namespace SistemaTransportes.Domain.Entities;

public class Encomienda
{
    public int EncomiendaID { get; set; }
    public int? BoletoID { get; set; }
    public int? ViajeID { get; set; }
    public int? CajaTurnoID { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal CostoCarga { get; set; }
    public DateTime FechaRecepcion { get; set; } = DateTime.Now;
    public string? RemitenteTipoDoc { get; set; } = "DNI";
    public string? RemitenteDoc { get; set; }
    public string? RemitenteNombre { get; set; }
    public string? RemitenteTelefono { get; set; }
    public string? DestinatarioTipoDoc { get; set; } = "DNI";
    public string? DestinatarioDoc { get; set; }
    public string? DestinatarioNombre { get; set; }
    public string? DestinatarioTelefono { get; set; }
    public string? ModalidadEntrega { get; set; } = "Agencia";
    public string? DireccionEntrega { get; set; }
    public decimal? RecargoDelivery { get; set; }
    public string? MetodoPago { get; set; } = "Efectivo";
    public string? NumeroOperacion { get; set; }
}
