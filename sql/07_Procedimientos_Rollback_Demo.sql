-- Script 07: Configuración de segundo operador para demostración de concurrencia y Rollback
USE [BD_Transportes];
SET NOCOUNT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Username NVARCHAR(40) = N'OpVenta2';
    DECLARE @PasswordPlain VARCHAR(50) = '1598753';
    DECLARE @PasswordHash NVARCHAR(256) = CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', @PasswordPlain), 2);
    DECLARE @Rol NVARCHAR(20) = N'Operador';
    DECLARE @Nombres NVARCHAR(100) = N'Segundo Operador (Ventanilla 2)';
    DECLARE @OpVenta2ID INT = NULL;

    -- 1. Registrar o actualizar credenciales de OpVenta2
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

    -- 2. Asegurar turno de caja activo e independiente para OpVenta2
    IF NOT EXISTS (SELECT 1 FROM dbo.CajasTurno WHERE UsuarioID = @OpVenta2ID AND Estado IN ('Abierto', 'Abierta'))
    BEGIN
        INSERT INTO dbo.CajasTurno (UsuarioID, MontoApertura, MontoActual, FechaApertura, Estado)
        VALUES (@OpVenta2ID, 500.00, 500.00, GETDATE(), N'Abierto');
    END;

    COMMIT TRANSACTION;
    PRINT 'Operador OpVenta2 y caja independiente configurados para demo de concurrencia.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
