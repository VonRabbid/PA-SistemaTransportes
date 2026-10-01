using SistemaTransportes.Application.DTOs;

namespace SistemaTransportes.Application.Interfaces;

public interface IVentaService
{
    Task RegistrarVentaAsync(RegistroVentaRequestDTO request);
}
