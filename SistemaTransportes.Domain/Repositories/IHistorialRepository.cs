using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface IHistorialRepository
{
    Task<IEnumerable<Boleto>> ObtenerBoletosPorTurnoAsync(int cajaTurnoId);
    Task<IEnumerable<Encomienda>> ObtenerEncomiendasPorTurnoAsync(int cajaTurnoId);
}
