using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;

namespace SistemaTransportes.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly ICajaTurnoRepository _cajaTurnoRepository;
    private readonly IHistorialRepository _historialRepository;

    public AuditoriaService(
        ICajaTurnoRepository cajaTurnoRepository,
        IHistorialRepository historialRepository)
    {
        _cajaTurnoRepository = cajaTurnoRepository ?? throw new ArgumentNullException(nameof(cajaTurnoRepository));
        _historialRepository = historialRepository ?? throw new ArgumentNullException(nameof(historialRepository));
    }

    public async Task<AuditoriaTurnoDTO> ObtenerResumenTurnoAsync(int cajaTurnoId)
    {
        decimal saldoActual = await _cajaTurnoRepository.ObtenerSaldoActualAsync(cajaTurnoId);
        var boletos = (await _historialRepository.ObtenerBoletosPorTurnoAsync(cajaTurnoId)).ToList();
        var encomiendas = (await _historialRepository.ObtenerEncomiendasPorTurnoAsync(cajaTurnoId)).ToList();

        decimal recaudadoBoletos = boletos.Sum(b => b.PrecioFinal);
        decimal recaudadoEncomiendas = encomiendas.Sum(e => e.CostoCarga);

        return new AuditoriaTurnoDTO
        {
            CajaTurnoId = cajaTurnoId,
            SaldoActual = saldoActual,
            TotalBoletos = boletos.Count,
            TotalEncomiendas = encomiendas.Count,
            TotalRecaudado = recaudadoBoletos + recaudadoEncomiendas
        };
    }

    public Task<IEnumerable<Boleto>> ObtenerBoletosTurnoAsync(int cajaTurnoId)
    {
        return _historialRepository.ObtenerBoletosPorTurnoAsync(cajaTurnoId);
    }

    public Task<IEnumerable<Encomienda>> ObtenerEncomiendasTurnoAsync(int cajaTurnoId)
    {
        return _historialRepository.ObtenerEncomiendasPorTurnoAsync(cajaTurnoId);
    }
}
