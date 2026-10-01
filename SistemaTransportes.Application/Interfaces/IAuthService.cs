using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Application.Interfaces;

public interface IAuthService
{
    Task<(Usuario Usuario, CajaTurno? TurnoActivo)?> IniciarSesionAsync(string username, string password);
}
