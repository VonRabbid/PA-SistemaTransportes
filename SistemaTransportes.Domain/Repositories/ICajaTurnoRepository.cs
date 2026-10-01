using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface ICajaTurnoRepository
{
    Task<CajaTurno?> ObtenerTurnoActivoAsync(int usuarioId);
    Task<decimal> ObtenerSaldoActualAsync(int cajaTurnoId);
}
