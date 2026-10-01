using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface IViajeRepository
{
    Task<IEnumerable<Viaje>> BuscarViajesAsync(string? origen, string? destino, DateTime fecha);
    Task<IEnumerable<string>> ObtenerOrigenesAsync();
    Task<IEnumerable<string>> ObtenerDestinosPorOrigenAsync(string origen);
    Task<Viaje?> ObtenerPorIdAsync(int viajeId);
}
