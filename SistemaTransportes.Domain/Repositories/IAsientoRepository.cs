using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface IAsientoRepository
{
    Task<IEnumerable<EstadoAsientoViaje>> ObtenerDisponibilidadAsientosAsync(int viajeId);
}
