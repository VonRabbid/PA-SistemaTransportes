using System.Security.Cryptography;
using System.Text;
using SistemaTransportes.Application.Interfaces;

namespace SistemaTransportes.Application.Services;

public class SecurityService : ISecurityService
{
    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            return string.Empty;
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword));
        return Convert.ToHexString(bytes);
    }
}
