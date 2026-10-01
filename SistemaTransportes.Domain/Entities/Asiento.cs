namespace SistemaTransportes.Domain.Entities;

public class Asiento
{
    public int AsientoID { get; set; }
    public int BusID { get; set; }
    public int NroAsiento { get; set; }
    public int Piso { get; set; } = 1;
}
