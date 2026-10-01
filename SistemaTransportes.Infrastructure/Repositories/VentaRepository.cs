using System.Data;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Exceptions;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;

namespace SistemaTransportes.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VentaRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task RegistrarVentaTransaccionalAsync(
        IEnumerable<Boleto> boletos,
        Encomienda? encomienda,
        int cajaTurnoId,
        decimal montoTotal)
    {
        var listaBoletos = boletos?.ToList() ?? new List<Boleto>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. Validar que la caja esté abierta y activa con bloqueo exclusivo
            const string sqlCheckCaja = @"
                SELECT Estado 
                FROM dbo.CajasTurno WITH (UPDLOCK, ROWLOCK) 
                WHERE CajaTurnoID = @CajaTurnoID;";

            using (var cmdCaja = new SqlCommand(sqlCheckCaja, connection, transaction))
            {
                cmdCaja.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });
                var estadoObj = await cmdCaja.ExecuteScalarAsync();

                if (estadoObj == null || estadoObj == DBNull.Value)
                {
                    throw new CajaNoActivaException($"La caja #{cajaTurnoId} no existe.");
                }

                string estadoCaja = estadoObj.ToString()?.Trim() ?? string.Empty;
                if (!string.Equals(estadoCaja, "Abierto", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(estadoCaja, "Abierta", StringComparison.OrdinalIgnoreCase))
                {
                    throw new CajaNoActivaException($"La caja #{cajaTurnoId} no se encuentra activa (Estado: {estadoCaja}).");
                }
            }

            // 2. Validar disponibilidad de asientos y bloquear con UPDLOCK, ROWLOCK
            foreach (var b in listaBoletos)
            {
                const string sqlCheckAsiento = @"
                    SELECT Estado 
                    FROM dbo.EstadoAsientosViaje WITH (UPDLOCK, ROWLOCK) 
                    WHERE ViajeID = @ViajeID AND NroAsiento = @NroAsiento;";

                using (var cmdAsiento = new SqlCommand(sqlCheckAsiento, connection, transaction))
                {
                    cmdAsiento.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = b.ViajeID });
                    cmdAsiento.Parameters.Add(new SqlParameter("@NroAsiento", SqlDbType.Int) { Value = b.NroAsiento });

                    var estadoSeatObj = await cmdAsiento.ExecuteScalarAsync();
                    if (estadoSeatObj == null || estadoSeatObj == DBNull.Value)
                    {
                        throw new AsientoNoDisponibleException($"El asiento {b.NroAsiento} no existe en el itinerario de viaje.");
                    }

                    string estadoAsiento = estadoSeatObj.ToString()?.Trim() ?? string.Empty;
                    if (!string.Equals(estadoAsiento, "Libre", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new AsientoNoDisponibleException($"El asiento {b.NroAsiento} ya no se encuentra disponible (Estado: {estadoAsiento}).");
                    }
                }

                const string sqlUpdateAsiento = @"
                    UPDATE dbo.EstadoAsientosViaje WITH (ROWLOCK)
                    SET Estado = 'Ocupado'
                    WHERE ViajeID = @ViajeID AND NroAsiento = @NroAsiento;";

                using (var cmdUpdateSeat = new SqlCommand(sqlUpdateAsiento, connection, transaction))
                {
                    cmdUpdateSeat.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = b.ViajeID });
                    cmdUpdateSeat.Parameters.Add(new SqlParameter("@NroAsiento", SqlDbType.Int) { Value = b.NroAsiento });
                    int affected = await cmdUpdateSeat.ExecuteNonQueryAsync();
                    if (affected == 0)
                    {
                        throw new AsientoNoDisponibleException($"No se pudo reservar el asiento {b.NroAsiento}.");
                    }
                }
            }

            // 3. Insertar boletos en dbo.Boletos
            int? primerBoletoId = null;
            foreach (var b in listaBoletos)
            {
                const string sqlInsertBoleto = @"
                    INSERT INTO dbo.Boletos (
                        ViajeID, NroAsiento, DniPasajero, NombrePasajero, 
                        PrecioFinal, FechaEmision, CajaTurnoID, MetodoPago, NumeroOperacion
                    ) VALUES (
                        @ViajeID, @NroAsiento, @DniPasajero, @NombrePasajero, 
                        @PrecioFinal, @FechaEmision, @CajaTurnoID, @MetodoPago, @NumeroOperacion
                    );
                    SELECT SCOPE_IDENTITY();";

                using var cmdBoleto = new SqlCommand(sqlInsertBoleto, connection, transaction);
                cmdBoleto.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = b.ViajeID });
                cmdBoleto.Parameters.Add(new SqlParameter("@NroAsiento", SqlDbType.Int) { Value = b.NroAsiento });
                cmdBoleto.Parameters.Add(new SqlParameter("@DniPasajero", SqlDbType.NVarChar, 8) { Value = b.DniPasajero ?? string.Empty });
                cmdBoleto.Parameters.Add(new SqlParameter("@NombrePasajero", SqlDbType.NVarChar, 100) { Value = b.NombrePasajero ?? string.Empty });
                cmdBoleto.Parameters.Add(new SqlParameter("@PrecioFinal", SqlDbType.Decimal) { Value = b.PrecioFinal });
                cmdBoleto.Parameters.Add(new SqlParameter("@FechaEmision", SqlDbType.DateTime) { Value = b.FechaEmision == default ? DateTime.Now : b.FechaEmision });
                cmdBoleto.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });
                cmdBoleto.Parameters.Add(new SqlParameter("@MetodoPago", SqlDbType.VarChar, 30) { Value = (object?)b.MetodoPago ?? DBNull.Value });
                cmdBoleto.Parameters.Add(new SqlParameter("@NumeroOperacion", SqlDbType.VarChar, 50) { Value = (object?)b.NumeroOperacion ?? DBNull.Value });

                try
                {
                    var idObj = await cmdBoleto.ExecuteScalarAsync();
                    int nuevoId = Convert.ToInt32(idObj);
                    b.BoletoID = nuevoId;
                    primerBoletoId ??= nuevoId;
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627 || ex.Message.Contains("UQ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AsientoNoDisponibleException($"El asiento {b.NroAsiento} ya cuenta con un boleto emitido (conflicto de concurrencia UQ).", ex);
                }
            }

            // 4. Insertar encomienda si existe
            if (encomienda != null)
            {
                const string sqlInsertEncomienda = @"
                    INSERT INTO dbo.Encomiendas (
                        BoletoID, ViajeID, CajaTurnoID, Descripcion, PesoKg, CostoCarga, FechaRecepcion,
                        RemitenteTipoDoc, RemitenteDoc, RemitenteNombre, RemitenteTelefono,
                        DestinatarioTipoDoc, DestinatarioDoc, DestinatarioNombre, DestinatarioTelefono,
                        ModalidadEntrega, DireccionEntrega, RecargoDelivery, MetodoPago, NumeroOperacion
                    ) VALUES (
                        @BoletoID, @ViajeID, @CajaTurnoID, @Descripcion, @PesoKg, @CostoCarga, @FechaRecepcion,
                        @RemitenteTipoDoc, @RemitenteDoc, @RemitenteNombre, @RemitenteTelefono,
                        @DestinatarioTipoDoc, @DestinatarioDoc, @DestinatarioNombre, @DestinatarioTelefono,
                        @ModalidadEntrega, @DireccionEntrega, @RecargoDelivery, @MetodoPago, @NumeroOperacion
                    );
                    SELECT SCOPE_IDENTITY();";

                using var cmdEnc = new SqlCommand(sqlInsertEncomienda, connection, transaction);
                cmdEnc.Parameters.Add(new SqlParameter("@BoletoID", SqlDbType.Int) { Value = (object?)primerBoletoId ?? (object?)encomienda.BoletoID ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@ViajeID", SqlDbType.Int) { Value = (object?)encomienda.ViajeID ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });
                cmdEnc.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 150) { Value = encomienda.Descripcion ?? string.Empty });
                cmdEnc.Parameters.Add(new SqlParameter("@PesoKg", SqlDbType.Decimal) { Value = encomienda.PesoKg });
                cmdEnc.Parameters.Add(new SqlParameter("@CostoCarga", SqlDbType.Decimal) { Value = encomienda.CostoCarga });
                cmdEnc.Parameters.Add(new SqlParameter("@FechaRecepcion", SqlDbType.DateTime) { Value = encomienda.FechaRecepcion == default ? DateTime.Now : encomienda.FechaRecepcion });
                cmdEnc.Parameters.Add(new SqlParameter("@RemitenteTipoDoc", SqlDbType.VarChar, 10) { Value = (object?)encomienda.RemitenteTipoDoc ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@RemitenteDoc", SqlDbType.VarChar, 15) { Value = (object?)encomienda.RemitenteDoc ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@RemitenteNombre", SqlDbType.NVarChar, 120) { Value = (object?)encomienda.RemitenteNombre ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@RemitenteTelefono", SqlDbType.VarChar, 15) { Value = (object?)encomienda.RemitenteTelefono ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@DestinatarioTipoDoc", SqlDbType.VarChar, 10) { Value = (object?)encomienda.DestinatarioTipoDoc ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@DestinatarioDoc", SqlDbType.VarChar, 15) { Value = (object?)encomienda.DestinatarioDoc ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@DestinatarioNombre", SqlDbType.NVarChar, 120) { Value = (object?)encomienda.DestinatarioNombre ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@DestinatarioTelefono", SqlDbType.VarChar, 15) { Value = (object?)encomienda.DestinatarioTelefono ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@ModalidadEntrega", SqlDbType.VarChar, 30) { Value = (object?)encomienda.ModalidadEntrega ?? "Agencia" });
                cmdEnc.Parameters.Add(new SqlParameter("@DireccionEntrega", SqlDbType.VarChar, 200) { Value = (object?)encomienda.DireccionEntrega ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@RecargoDelivery", SqlDbType.Decimal) { Value = (object?)encomienda.RecargoDelivery ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@MetodoPago", SqlDbType.VarChar, 30) { Value = (object?)encomienda.MetodoPago ?? DBNull.Value });
                cmdEnc.Parameters.Add(new SqlParameter("@NumeroOperacion", SqlDbType.VarChar, 50) { Value = (object?)encomienda.NumeroOperacion ?? DBNull.Value });

                var encIdObj = await cmdEnc.ExecuteScalarAsync();
                encomienda.EncomiendaID = Convert.ToInt32(encIdObj);
            }

            // 5. Actualizar MontoActual en dbo.CajasTurno
            const string sqlUpdateCaja = @"
                UPDATE dbo.CajasTurno WITH (ROWLOCK)
                SET MontoActual = MontoActual + @MontoTotal 
                WHERE CajaTurnoID = @CajaTurnoID;";

            using (var cmdUpdateCaja = new SqlCommand(sqlUpdateCaja, connection, transaction))
            {
                cmdUpdateCaja.Parameters.Add(new SqlParameter("@MontoTotal", SqlDbType.Decimal) { Value = montoTotal });
                cmdUpdateCaja.Parameters.Add(new SqlParameter("@CajaTurnoID", SqlDbType.Int) { Value = cajaTurnoId });
                await cmdUpdateCaja.ExecuteNonQueryAsync();
            }

            // 6. Confirmar la transacción
            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Ignorar excepciones al revertir si la conexión ya está cerrada o abortada por SQL
            }
            throw;
        }
    }
}
