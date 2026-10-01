using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Exceptions;
using SistemaTransportes.Infrastructure.Connection;
using SistemaTransportes.Infrastructure.Repositories;

namespace SistemaTransportes.Regression;

internal static class SqlChecks
{
    internal static async Task RunAsync()
    {
        // Solo instancia local, autenticación integrada; nunca usa credenciales del producto.
        const string masterString = "Server=.;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10";
        string name = "TransportesAudit_" + Guid.NewGuid().ToString("N");
        await using var master = new SqlConnection(masterString);
        await master.OpenAsync();
        bool created = false;
        try
        {
            using (var create = new SqlCommand($"CREATE DATABASE [{name}]", master)) await create.ExecuteNonQueryAsync();
            created = true;
            string connection = new SqlConnectionStringBuilder(masterString) { InitialCatalog = name }.ConnectionString;
            await using var db = new SqlConnection(connection);
            await db.OpenAsync();
            foreach (var path in Directory.GetFiles("sql", "*.sql").Order())
            {
                string script = File.ReadAllText(path).Replace("BD_Transportes", name);
                foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    using var command = new SqlCommand(batch, db) { CommandTimeout = 60 };
                    await command.ExecuteNonQueryAsync();
                }
                Program.Check(true, "SQL ejecuta " + Path.GetFileName(path));
            }
            using (var seed = new SqlCommand(@"
                INSERT dbo.Usuarios(Username,PasswordHash,Rol,Activo) VALUES(N'Audit',N'NoLogin','Operador',1);
                DECLARE @u int = SCOPE_IDENTITY();
                INSERT dbo.CajasTurno(UsuarioID,MontoApertura,MontoActual,Estado) VALUES(@u,500,500,'Abierto');
                DECLARE @c int = SCOPE_IDENTITY();
                INSERT dbo.Buses(Placa,Capacidad) VALUES('AUD-TEST',4);
                DECLARE @b int = SCOPE_IDENTITY();
                INSERT dbo.Asientos(BusID,NroAsiento,Piso) VALUES(@b,1,1),(@b,2,1),(@b,3,1),(@b,4,1);
                INSERT dbo.Viajes(BusID,Origen,Destino,FechaSalida,PrecioBase) VALUES(@b,N'Lima',N'Huancayo',DATEADD(day,1,GETDATE()),65);
                DECLARE @v int = SCOPE_IDENTITY();
                INSERT dbo.EstadoAsientosViaje(ViajeID,NroAsiento,Estado) VALUES(@v,1,'Libre'),(@v,2,'Libre'),(@v,3,'Libre'),(@v,4,'Libre');
                SELECT @c, @v;", db))
            {
                using var reader = await seed.ExecuteReaderAsync();
                await reader.ReadAsync();
                _caja = reader.GetInt32(0); _viaje = reader.GetInt32(1);
            }
            var factory = new Factory(connection);
            var repo = new VentaRepository(factory);
            var parcel = new Encomienda { ViajeID = _viaje, CajaTurnoID = _caja, Descripcion = "Documentación", PesoKg = 1, CostoCarga = 8, RemitenteNombre = "José Núñez 李", DestinatarioNombre = "María Peña", ModalidadEntrega = "Agencia" };
            await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(1) }, parcel, _caja, 73);
            Program.Check(await Scalar(db, "SELECT MontoActual FROM dbo.CajasTurno WHERE CajaTurnoID=" + _caja) == 573, "Commit integra boleto, encomienda y caja");
            Program.Check((await new HistorialRepository(factory).ObtenerEncomiendasPorTurnoAsync(_caja)).Single().RemitenteNombre == "José Núñez 李", "SQL conserva nombres Unicode");

            // El segundo asiento del lote ya está vendido: el primero debe revertirse.
            try { await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(2), Ticket(1) }, null, _caja, 130); throw new Exception("Conflicto aceptado"); }
            catch (AsientoNoDisponibleException) { }
            Program.Check(await Scalar(db, $"SELECT COUNT(*) FROM dbo.Boletos WHERE ViajeID={_viaje}") == 1 && await Scalar(db, $"SELECT COUNT(*) FROM dbo.EstadoAsientosViaje WHERE ViajeID={_viaje} AND NroAsiento=2 AND Estado='Libre'") == 1, "Conflicto conserva boleto previo y revierte lote");

            // Fallo después de insertar boleto: la encomienda viola CHECK y revierte todo.
            parcel.PesoKg = -1;
            try { await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(2) }, parcel, _caja, 73); throw new Exception("Peso inválido aceptado por SQL"); }
            catch (SqlException) { }
            Program.Check(await Scalar(db, $"SELECT COUNT(*) FROM dbo.Boletos WHERE ViajeID={_viaje}") == 1 && await Scalar(db, $"SELECT MontoActual FROM dbo.CajasTurno WHERE CajaTurnoID={_caja}") == 573, "Error de encomienda revierte boleto y saldo");

            // Dos conexiones compiten por la misma butaca.
            async Task<bool> Sell()
            {
                try { await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(2) }, null, _caja, 65); return true; }
                catch (AsientoNoDisponibleException) { return false; }
            }
            var results = await Task.WhenAll(Sell(), Sell());
            Program.Check(results.Count(x => x) == 1 && await Scalar(db, $"SELECT MontoActual FROM dbo.CajasTurno WHERE CajaTurnoID={_caja}") == 638, "Concurrencia: una venta, un incremento de caja");

            // Inconsistencia externa: libre en mapa, pero boleto existente; activa UQ.
            using (var reset = new SqlCommand($"UPDATE dbo.EstadoAsientosViaje SET Estado='Libre' WHERE ViajeID={_viaje} AND NroAsiento=2", db)) await reset.ExecuteNonQueryAsync();
            try { await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(2) }, null, _caja, 65); throw new Exception("UQ omitida"); }
            catch (AsientoNoDisponibleException) { Program.Check(true, "UQ_Boletos se traduce a conflicto comprensible"); }

            using (var close = new SqlCommand($"UPDATE dbo.CajasTurno SET Estado='Cerrada' WHERE CajaTurnoID={_caja}", db)) await close.ExecuteNonQueryAsync();
            try { await repo.RegistrarVentaTransaccionalAsync(new[] { Ticket(3) }, null, _caja, 65); throw new Exception("Caja cerrada aceptada"); }
            catch (CajaNoActivaException) { }
            Program.Check(await Scalar(db, $"SELECT COUNT(*) FROM dbo.Boletos WHERE ViajeID={_viaje} AND NroAsiento=3") == 0, "Caja cerrada impide emitir y cobrar");
            await db.CloseAsync();
        }
        finally
        {
            if (created)
            {
                SqlConnection.ClearAllPools();
                using var drop = new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];", master);
                await drop.ExecuteNonQueryAsync();
                Console.WriteLine("Base temporal de pruebas eliminada: " + name);
            }
        }
    }
    private static int _caja, _viaje;
    private static Boleto Ticket(int seat) => new() { ViajeID = _viaje, CajaTurnoID = _caja, NroAsiento = seat, DniPasajero = "12345678", NombrePasajero = "José Núñez", PrecioFinal = 65 };
    private static async Task<decimal> Scalar(SqlConnection db, string sql)
    {
        using var command = new SqlCommand(sql, db);
        return Convert.ToDecimal(await command.ExecuteScalarAsync());
    }
    private sealed class Factory(string connection) : ISqlConnectionFactory
    {
        public SqlConnection CreateConnection() => new(connection);
    }
}
