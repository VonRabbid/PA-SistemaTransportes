using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class ViajeRepository : IVentaRepositoryViajes, IViajeRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ViajeRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<Viaje>> BuscarViajesAsync(string? origen, string? destino, DateTime fecha)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT v.ViajeID, v.BusID, v.Origen, v.Destino, v.TipoServicio, 
                   v.FechaSalida, v.FechaHoraLlegada, v.Categoria, v.DuracionEstimada, 
                   v.PrecioBase, b.Placa
            FROM dbo.Viajes v
            INNER JOIN dbo.Buses b ON v.BusID = b.BusID
            WHERE (@Origen IS NULL OR v.Origen = @Origen)
              AND (@Destino IS NULL OR v.Destino = @Destino)
            ORDER BY v.FechaSalida ASC;";

        string? origenParam = string.IsNullOrWhiteSpace(origen) ? null : origen;
        string? destinoParam = string.IsNullOrWhiteSpace(destino) ? null : destino;

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@Origen", SqlDbType.NVarChar, 50) { Value = (object?)origenParam ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Destino", SqlDbType.NVarChar, 50) { Value = (object?)destinoParam ?? DBNull.Value });

        var lista = new List<Viaje>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Viaje
            {
                ViajeID = reader.GetInt32(0),
                BusID = reader.GetInt32(1),
                Origen = reader.GetString(2),
                Destino = reader.GetString(3),
                TipoServicio = reader.GetString(4),
                FechaSalida = reader.GetDateTime(5),
                FechaHoraLlegada = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                Categoria = reader.GetString(7),
                DuracionEstimada = reader.IsDBNull(8) ? null : reader.GetString(8),
                PrecioBase = reader.GetDecimal(9),
                Placa = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        }

        return lista;
    }

    public async Task<IEnumerable<string>> ObtenerOrigenesAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT DISTINCT Origen FROM dbo.Viajes ORDER BY Origen ASC;";
        using var cmd = new SqlCommand(sql, connection);

        var origenes = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                origenes.Add(reader.GetString(0));
            }
        }

        return origenes;
    }

    public async Task<IEnumerable<string>> ObtenerDestinosPorOrigenAsync(string origen)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT DISTINCT Destino 
            FROM dbo.Viajes 
            WHERE Origen = @Origen 
            ORDER BY Destino ASC;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@Origen", SqlDbType.NVarChar, 50) { Value = origen });

        var destinos = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                destinos.Add(reader.GetString(0));
            }
        }

        return destinos;
    }

    public async Task<Viaje?> ObtenerPorIdAsync(int viajeId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT ViajeID, BusID, Origen, Destino, TipoServicio, 
                   FechaSalida, FechaHoraLlegada, Categoria, DuracionEstimada, PrecioBase
            FROM dbo.Viajes 
            WHERE ViajeID = @ViajeID;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = viajeId });

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Viaje
            {
                ViajeID = reader.GetInt32(0),
                BusID = reader.GetInt32(1),
                Origen = reader.GetString(2),
                Destino = reader.GetString(3),
                TipoServicio = reader.GetString(4),
                FechaSalida = reader.GetDateTime(5),
                FechaHoraLlegada = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                Categoria = reader.GetString(7),
                DuracionEstimada = reader.IsDBNull(8) ? null : reader.GetString(8),
                PrecioBase = reader.GetDecimal(9)
            };
        }

        return null;
    }
}

// Interfaz auxiliar interna para compatibilidad
internal interface IVentaRepositoryViajes { }
