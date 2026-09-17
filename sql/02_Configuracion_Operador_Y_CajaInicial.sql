-- Script 02: Configuración del operador principal y apertura de caja inicial
USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Username NVARCHAR(40) = N'OpControl';
    DECLARE @PasswordPlain VARCHAR(50) = '1598753';
    DECLARE @PasswordHash NVARCHAR(256) = CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', @PasswordPlain), 2);
    DECLARE @Rol NVARCHAR(20) = N'Operador';
    DECLARE @Nombres NVARCHAR(100) = N'Operador de Control';
    DECLARE @OpControlID INT = NULL;

    -- 1. Registro del operador de boletería principal 'OpControl' con contraseña encriptada en SHA-256
    SELECT @OpControlID = UsuarioID 
    FROM dbo.Usuarios 
    WHERE Username = @Username;

    IF @OpControlID IS NULL
    BEGIN
        INSERT INTO dbo.Usuarios (Username, PasswordHash, Rol, Activo, Nombres)
        VALUES (@Username, @PasswordHash, @Rol, 1, @Nombres);

        SET @OpControlID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Usuarios
        SET PasswordHash = @PasswordHash,
            Rol = @Rol,
            Activo = 1,
            Nombres = @Nombres
        WHERE UsuarioID = @OpControlID;
    END;

    -- Reasignación de turnos y boletos de operadores previos hacia el operador principal
    UPDATE dbo.CajasTurno
    SET UsuarioID = @OpControlID
    WHERE UsuarioID <> @OpControlID;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Boletos') AND name = N'UsuarioID')
    BEGIN
        DECLARE @sqlReasignarBoletos NVARCHAR(MAX) = N'
            UPDATE dbo.Boletos 
            SET UsuarioID = ' + CAST(@OpControlID AS NVARCHAR(10)) + N' 
            WHERE UsuarioID <> ' + CAST(@OpControlID AS NVARCHAR(10));
        EXEC sp_executesql @sqlReasignarBoletos;
    END;

    -- Limpieza de usuarios de prueba para mantener un único operador activo
    DELETE FROM dbo.Usuarios 
    WHERE Username <> @Username;

    -- 2. Apertura del turno inicial de caja con saldo base de S/. 500.00 para iniciar operaciones de venta
    DECLARE @CajaID INT = NULL;

    SELECT TOP 1 @CajaID = CajaTurnoID 
    FROM dbo.CajasTurno 
    WHERE UsuarioID = @OpControlID 
    ORDER BY CajaTurnoID ASC;

    IF @CajaID IS NULL
    BEGIN
        INSERT INTO dbo.CajasTurno (UsuarioID, MontoApertura, MontoActual, FechaApertura, Estado)
        VALUES (@OpControlID, 500.00, 500.00, GETDATE(), N'Abierto');

        SET @CajaID = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        -- Reasignar boletos y encomiendas al turno de caja activo
        UPDATE dbo.Boletos
        SET CajaTurnoID = @CajaID
        WHERE CajaTurnoID <> @CajaID;

        UPDATE dbo.Encomiendas
        SET CajaTurnoID = @CajaID
        WHERE CajaTurnoID IS NOT NULL AND CajaTurnoID <> @CajaID;

        -- Eliminar turnos de caja duplicados o cerrados anteriormente
        DELETE FROM dbo.CajasTurno 
        WHERE CajaTurnoID <> @CajaID;

        -- Conciliar saldo acumulado de caja (apertura + boletos + encomiendas)
        DECLARE @TotalBoletosEmitidos DECIMAL(18,2) = 0.00;
        DECLARE @TotalEncomiendasEmitidas DECIMAL(18,2) = 0.00;

        SELECT @TotalBoletosEmitidos = ISNULL(SUM(PrecioFinal), 0.00) 
        FROM dbo.Boletos 
        WHERE CajaTurnoID = @CajaID;

        SELECT @TotalEncomiendasEmitidas = ISNULL(SUM(CostoCarga + ISNULL(RecargoDelivery, 0.00)), 0.00)
        FROM dbo.Encomiendas
        WHERE CajaTurnoID = @CajaID;

        DECLARE @SaldoCalculado DECIMAL(18,2) = 500.00 + @TotalBoletosEmitidos + @TotalEncomiendasEmitidas;

        UPDATE dbo.CajasTurno
        SET UsuarioID = @OpControlID,
            MontoApertura = 500.00,
            MontoActual = @SaldoCalculado,
            Estado = N'Abierto'
        WHERE CajaTurnoID = @CajaID;
    END;

    COMMIT TRANSACTION;
    PRINT 'Operador OpControl y caja inicial configurados correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
