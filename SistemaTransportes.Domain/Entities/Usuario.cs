namespace SistemaTransportes.Domain.Entities;

public class Usuario
{
    public int UsuarioID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Operador";
    public bool Activo { get; set; } = true;
    public string? Nombres { get; set; }
}
