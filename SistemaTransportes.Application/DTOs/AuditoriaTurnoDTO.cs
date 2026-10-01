namespace SistemaTransportes.Application.DTOs;

public class AuditoriaTurnoDTO
{
    public int CajaTurnoId { get; set; }
    public decimal SaldoActual { get; set; }
    public int TotalBoletos { get; set; }
    public int TotalEncomiendas { get; set; }
    public decimal TotalRecaudado { get; set; }
}
