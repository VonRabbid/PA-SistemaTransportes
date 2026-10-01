using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Application.Interfaces;

public interface IAuditoriaService
{
    Task<AuditoriaTurnoDTO> ObtenerResumenTurnoAsync(int cajaTurnoId);
    Task<IEnumerable<Boleto>> ObtenerBoletosTurnoAsync(int cajaTurnoId);
    Task<IEnumerable<Encomienda>> ObtenerEncomiendasTurnoAsync(int cajaTurnoId);
}
