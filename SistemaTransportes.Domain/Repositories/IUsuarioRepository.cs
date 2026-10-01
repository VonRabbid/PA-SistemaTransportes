using SistemaTransportes.Domain.Entities;

namespace SistemaTransportes.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ValidarCredencialesAsync(string username, string passwordHash);
}
