namespace SistemaTransportes.Domain.Entities;

public class CajaTurno
{
    public int CajaTurnoID { get; set; }
    public int UsuarioID { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal MontoActual { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.Now;
    public string Estado { get; set; } = "Abierto";
}
