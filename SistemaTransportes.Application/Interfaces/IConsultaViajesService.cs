using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Application.Interfaces;

public interface IConsultaViajesService
{
    Task<IEnumerable<ViajeDTO>> BuscarViajesAsync(string? origen, string? destino, DateTime fecha);
    Task<IEnumerable<string>> ObtenerOrigenesAsync();
    Task<IEnumerable<string>> ObtenerDestinosPorOrigenAsync(string origen);
    Task<IEnumerable<EstadoAsientoViaje>> ObtenerMapaAsientosAsync(int viajeId);
}
