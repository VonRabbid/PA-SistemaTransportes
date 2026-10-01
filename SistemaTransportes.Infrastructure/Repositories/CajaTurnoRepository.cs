using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class CajaTurnoRepository : ICajaTurnoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CajaTurnoRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<CajaTurno?> ObtenerTurnoActivoAsync(int usuarioId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT TOP 1 
                CajaTurnoID, 
                UsuarioID, 
                MontoApertura, 
                MontoActual, 
                FechaApertura, 
                Estado 
            FROM dbo.CajasTurno 
            WHERE UsuarioID = @UsuarioID 
              AND Estado IN ('Abierto', 'Abierta')
            ORDER BY CajaTurnoID DESC;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@UsuarioID", SqlDbType.Int) { Value = usuarioId });

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CajaTurno
            {
                CajaTurnoID = reader.GetInt32(0),
                UsuarioID = reader.GetInt32(1),
                MontoApertura = reader.GetDecimal(2),
                MontoActual = reader.GetDecimal(3),
                FechaApertura = reader.GetDateTime(4),
                Estado = reader.GetString(5)
            };
        }

        return null;
    }

    public async Task<decimal> ObtenerSaldoActualAsync(int cajaTurnoId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT MontoActual 
            FROM dbo.CajasTurno 
            WHERE CajaTurnoID = @CajaTurnoID;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });

        var result = await cmd.ExecuteScalarAsync();
        if (result != null && result != DBNull.Value)
        {
            return Convert.ToDecimal(result);
        }

        return 0m;
    }
}
