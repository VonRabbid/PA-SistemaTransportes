using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;

namespace SistemaTransportes.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICajaTurnoRepository _cajaTurnoRepository;
    private readonly ISecurityService _securityService;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        ICajaTurnoRepository cajaTurnoRepository,
        ISecurityService securityService)
    {
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _cajaTurnoRepository = cajaTurnoRepository ?? throw new ArgumentNullException(nameof(cajaTurnoRepository));
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
    }

    public async Task<(Usuario Usuario, CajaTurno? TurnoActivo)?> IniciarSesionAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        string hash = _securityService.HashPassword(password);
        var usuario = await _usuarioRepository.ValidarCredencialesAsync(username.Trim(), hash);

        if (usuario == null || !usuario.Activo)
        {
            return null;
        }

        var turno = await _cajaTurnoRepository.ObtenerTurnoActivoAsync(usuario.UsuarioID);
        return (usuario, turno);
    }
}
