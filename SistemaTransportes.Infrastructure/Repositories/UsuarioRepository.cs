using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UsuarioRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Usuario?> ValidarCredencialesAsync(string username, string passwordHash)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                UsuarioID, 
                Username, 
                PasswordHash, 
                Rol, 
                Activo, 
                ISNULL(Nombres, Username) AS Nombres
            FROM dbo.Usuarios
            WHERE Username = @Username 
              AND PasswordHash = @PasswordHash 
              AND Activo = 1;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 40) { Value = username });
        cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = passwordHash });

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Usuario
            {
                UsuarioID = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Rol = reader.GetString(3),
                Activo = reader.GetBoolean(4),
                Nombres = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return null;
    }
}
