-- Script 08: Configuración de concurrencia multioperador y protección de integridad ACID
USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Operador Ventanilla 2 (OpVenta2)
    DECLARE @Username NVARCHAR(40) = N'OpVenta2';
    DECLARE @PasswordHash NVARCHAR(256) = N'E24997BC4956B198CD45BE13E904791E8DDECEBA6AA4E3FB62D5366472251FD5';
    DECLARE @Rol NVARCHAR(20) = N'Operador';
    DECLARE @Nombres NVARCHAR(100) = N'Segundo Operador (Ventanilla 2)';
    DECLARE @OpVenta2ID INT = NULL;

    SELECT @OpVenta2ID = UsuarioID FROM dbo.Usuarios WHERE Username = @Username;
    IF @OpVenta2ID IS NULL
    BEGIN
        INSERT INTO dbo.Usuarios (Username, PasswordHash, Rol, Activo, Nombres)
        VALUES (@Username, @PasswordHash, @Rol, 1, @Nombres);
        SET @OpVenta2ID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Usuarios
        SET PasswordHash = @PasswordHash, Rol = @Rol, Activo = 1, Nombres = @Nombres
        WHERE UsuarioID = @OpVenta2ID;
    END;

    -- 2. Asegurar turno de caja activo e independiente para OpVenta2 (S/. 500.00)
    IF NOT EXISTS (SELECT 1 FROM dbo.CajasTurno WHERE UsuarioID = @OpVenta2ID AND Estado IN ('Abierto', 'Abierta'))
    BEGIN
        INSERT INTO dbo.CajasTurno (UsuarioID, MontoApertura, MontoActual, FechaApertura, Estado)
        VALUES (@OpVenta2ID, 500.00, 500.00, GETDATE(), N'Abierto');
    END;

    -- 3. Crear índice único de concurrencia en Boletos (ViajeID, NroAsiento) si no existe
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Boletos_Viaje_Asiento')
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UQ_Boletos_Viaje_Asiento 
        ON dbo.Boletos (ViajeID, NroAsiento);
    END;

    COMMIT TRANSACTION;
    PRINT 'Configuración de concurrencia y restricción UQ_Boletos_Viaje_Asiento aplicadas correctamente.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
