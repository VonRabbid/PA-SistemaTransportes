namespace SistemaTransportes.Application.Interfaces;

public interface ISecurityService
{
    string HashPassword(string plainPassword);
}
