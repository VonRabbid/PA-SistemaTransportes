using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class AsientoRepository : IAsientoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AsientoRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<EstadoAsientoViaje>> ObtenerDisponibilidadAsientosAsync(int viajeId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT EstadoAsientoID, ViajeID, NroAsiento, Estado, RowVersion 
            FROM dbo.EstadoAsientosViaje 
            WHERE ViajeID = @ViajeID 
            ORDER BY NroAsiento ASC;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = viajeId });

        var lista = new List<EstadoAsientoViaje>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            byte[]? rowVersion = null;
            if (!reader.IsDBNull(4))
            {
                rowVersion = (byte[])reader[4];
            }

            lista.Add(new EstadoAsientoViaje
            {
                EstadoAsientoID = reader.GetInt32(0),
                ViajeID = reader.GetInt32(1),
                NroAsiento = reader.GetInt32(2),
                Estado = reader.GetString(3),
                RowVersion = rowVersion
            });
        }

        return lista;
    }
}
