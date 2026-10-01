namespace SistemaTransportes.UI.ViewModels;

public class BoletoHistorialItem
{
    public int BoletoID { get; set; }
    public string CodigoBoleto => $"BOL-{BoletoID:D5}";
    public DateTime FechaEmision { get; set; }
    public string FechaHoraStr => FechaEmision.ToString("dd/MM/yyyy HH:mm");
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Ruta => $"{Origen} ➔ {Destino}";
    public int NroAsiento { get; set; }
    public string AsientoStr => $"Asiento #{NroAsiento}";
    public string DniPasajero { get; set; } = string.Empty;
    public string NombrePasajero { get; set; } = string.Empty;
    public string PasajeroInfo => $"{DniPasajero} - {NombrePasajero}";
    public decimal PrecioFinal { get; set; }
    public string PrecioStr => $"S/. {PrecioFinal:N2}";
    public string MetodoPago { get; set; } = "Efectivo";
    public string NumeroOperacion { get; set; } = string.Empty;
    public string OperacionStr => string.IsNullOrWhiteSpace(NumeroOperacion) ? "-" : NumeroOperacion;
}

public class EncomiendaHistorialItem
{
    public int EncomiendaID { get; set; }
    public string CodigoEncomienda => $"GUIA-{EncomiendaID:D5}";
    public DateTime FechaRecepcion { get; set; }
    public string FechaHoraStr => FechaRecepcion.ToString("dd/MM/yyyy HH:mm");
    public string RemitenteDoc { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = string.Empty;
    public string RemitenteInfo => string.IsNullOrWhiteSpace(RemitenteDoc) ? RemitenteNombre : $"{RemitenteDoc} - {RemitenteNombre}";
    public string DestinatarioDoc { get; set; } = string.Empty;
    public string DestinatarioNombre { get; set; } = string.Empty;
    public string DestinatarioInfo => string.IsNullOrWhiteSpace(DestinatarioDoc) ? DestinatarioNombre : $"{DestinatarioDoc} - {DestinatarioNombre}";
    public string ModalidadEntrega { get; set; } = "Agencia";
    public string Descripcion { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public string PesoStr => $"{PesoKg:N1} kg";
    public decimal TotalCarga { get; set; }
    public string CostoTotalStr => $"S/. {TotalCarga:N2}";
    public string MetodoPago { get; set; } = "Efectivo";
    public string NumeroOperacion { get; set; } = string.Empty;
    public string MetodoPagoInfo => string.IsNullOrWhiteSpace(NumeroOperacion) ? MetodoPago : $"{MetodoPago} (Ref: {NumeroOperacion})";
}
