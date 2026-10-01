using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;

namespace SistemaTransportes.Application.Services;

public class ConsultaViajesService : IConsultaViajesService
{
    private readonly IViajeRepository _viajeRepository;
    private readonly IAsientoRepository _asientoRepository;

    public ConsultaViajesService(
        IViajeRepository viajeRepository,
        IAsientoRepository asientoRepository)
    {
        _viajeRepository = viajeRepository ?? throw new ArgumentNullException(nameof(viajeRepository));
        _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
    }

    public async Task<IEnumerable<ViajeDTO>> BuscarViajesAsync(string? origen, string? destino, DateTime fecha)
    {
        var viajes = await _viajeRepository.BuscarViajesAsync(origen, destino, fecha);
        var lista = new List<ViajeDTO>();

        foreach (var v in viajes)
        {
            var asientos = await _asientoRepository.ObtenerDisponibilidadAsientosAsync(v.ViajeID);
            int libres = asientos.Count(a => string.Equals(a.Estado, "Libre", StringComparison.OrdinalIgnoreCase));

            lista.Add(new ViajeDTO
            {
                ViajeID = v.ViajeID,
                BusID = v.BusID,
                Origen = v.Origen,
                Destino = v.Destino,
                TipoServicio = v.TipoServicio,
                FechaSalida = v.FechaSalida,
                FechaHoraLlegada = v.FechaHoraLlegada,
                Categoria = v.Categoria,
                DuracionEstimada = v.DuracionEstimada,
                PrecioBase = v.PrecioBase,
                PlacaBus = v.Placa ?? string.Empty,
                AsientosLibres = libres
            });
        }

        return lista;
    }

    public Task<IEnumerable<string>> ObtenerOrigenesAsync()
    {
        return _viajeRepository.ObtenerOrigenesAsync();
    }

    public Task<IEnumerable<string>> ObtenerDestinosPorOrigenAsync(string origen)
    {
        return _viajeRepository.ObtenerDestinosPorOrigenAsync(origen);
    }

    public Task<IEnumerable<EstadoAsientoViaje>> ObtenerMapaAsientosAsync(int viajeId)
    {
        return _asientoRepository.ObtenerDisponibilidadAsientosAsync(viajeId);
    }
}
