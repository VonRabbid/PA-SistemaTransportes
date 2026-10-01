namespace SistemaTransportes.Domain.Entities;

public class Bus
{
    public int BusID { get; set; }
    public string Placa { get; set; } = string.Empty;
    public int Capacidad { get; set; }
}
