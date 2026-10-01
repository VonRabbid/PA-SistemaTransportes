using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class HistorialRepository : IHistorialRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public HistorialRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<Boleto>> ObtenerBoletosPorTurnoAsync(int cajaTurnoId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                b.BoletoID, 
                b.ViajeID, 
                b.NroAsiento, 
                b.DniPasajero, 
                b.NombrePasajero, 
                b.PrecioFinal, 
                b.FechaEmision, 
                b.CajaTurnoID, 
                b.MetodoPago, 
                b.NumeroOperacion
            FROM dbo.Boletos b
            WHERE b.CajaTurnoID = @CajaTurnoID
            ORDER BY b.BoletoID DESC;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });

        var boletos = new List<Boleto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            boletos.Add(new Boleto
            {
                BoletoID = reader.GetInt32(0),
                ViajeID = reader.GetInt32(1),
                NroAsiento = reader.GetInt32(2),
                DniPasajero = reader.GetString(3),
                NombrePasajero = reader.GetString(4),
                PrecioFinal = reader.GetDecimal(5),
                FechaEmision = reader.GetDateTime(6),
                CajaTurnoID = reader.GetInt32(7),
                MetodoPago = reader.IsDBNull(8) ? "Efectivo" : reader.GetString(8),
                NumeroOperacion = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return boletos;
    }

    public async Task<IEnumerable<Encomienda>> ObtenerEncomiendasPorTurnoAsync(int cajaTurnoId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                EncomiendaID, 
                BoletoID, 
                ViajeID, 
                CajaTurnoID, 
                Descripcion, 
                PesoKg, 
                CostoCarga, 
                FechaRecepcion,
                RemitenteTipoDoc, 
                RemitenteDoc, 
                RemitenteNombre, 
                RemitenteTelefono,
                DestinatarioTipoDoc, 
                DestinatarioDoc, 
                DestinatarioNombre, 
                DestinatarioTelefono,
                ModalidadEntrega, 
                DireccionEntrega, 
                RecargoDelivery, 
                MetodoPago, 
                NumeroOperacion
            FROM dbo.Encomiendas
            WHERE CajaTurnoID = @CajaTurnoID
            ORDER BY EncomiendaID DESC;";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });

        var encomiendas = new List<Encomienda>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            encomiendas.Add(new Encomienda
            {
                EncomiendaID = reader.GetInt32(0),
                BoletoID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ViajeID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                CajaTurnoID = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Descripcion = reader.GetString(4),
                PesoKg = reader.GetDecimal(5),
                CostoCarga = reader.GetDecimal(6),
                FechaRecepcion = reader.GetDateTime(7),
                RemitenteTipoDoc = reader.IsDBNull(8) ? null : reader.GetString(8),
                RemitenteDoc = reader.IsDBNull(9) ? null : reader.GetString(9),
                RemitenteNombre = reader.IsDBNull(10) ? null : reader.GetString(10),
                RemitenteTelefono = reader.IsDBNull(11) ? null : reader.GetString(11),
                DestinatarioTipoDoc = reader.IsDBNull(12) ? null : reader.GetString(12),
                DestinatarioDoc = reader.IsDBNull(13) ? null : reader.GetString(13),
                DestinatarioNombre = reader.IsDBNull(14) ? null : reader.GetString(14),
                DestinatarioTelefono = reader.IsDBNull(15) ? null : reader.GetString(15),
                ModalidadEntrega = reader.IsDBNull(16) ? "Agencia" : reader.GetString(16),
                DireccionEntrega = reader.IsDBNull(17) ? null : reader.GetString(17),
                RecargoDelivery = reader.IsDBNull(18) ? null : reader.GetDecimal(18),
                MetodoPago = reader.IsDBNull(19) ? "Efectivo" : reader.GetString(19),
                NumeroOperacion = reader.IsDBNull(20) ? null : reader.GetString(20)
            });
        }

        return encomiendas;
    }
}
