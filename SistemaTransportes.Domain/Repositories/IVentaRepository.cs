using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface IVentaRepository
{
    Task RegistrarVentaTransaccionalAsync(IEnumerable<Boleto> boletos, Encomienda? encomienda, int cajaTurnoId, decimal montoTotal);
}
