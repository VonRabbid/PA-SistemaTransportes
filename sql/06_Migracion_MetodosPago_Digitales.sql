-- Script 06: Soporte para métodos de pago y número de operación

USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- Soporte para métodos de pago digitales (Efectivo, Yape, Plin, Transferencias y Tarjetas) con registro del número de operación bancaria
    -- 1. Incorporación de campos en dbo.Boletos
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Boletos') AND name = N'MetodoPago'
    )
    BEGIN
        ALTER TABLE dbo.Boletos ADD MetodoPago VARCHAR(30) NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Boletos') AND name = N'NumeroOperacion'
    )
    BEGIN
        ALTER TABLE dbo.Boletos ADD NumeroOperacion VARCHAR(50) NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Boletos_MetodoPago' AND object_id = OBJECT_ID(N'dbo.Boletos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Boletos_MetodoPago 
        ON dbo.Boletos (MetodoPago) 
        INCLUDE (PrecioFinal, FechaEmision, CajaTurnoID);
    END;

    -- 2. Incorporación de campos en dbo.Encomiendas
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Encomiendas') AND name = N'MetodoPago'
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas ADD MetodoPago VARCHAR(30) NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Encomiendas') AND name = N'NumeroOperacion'
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas ADD NumeroOperacion VARCHAR(50) NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Encomiendas_MetodoPago' AND object_id = OBJECT_ID(N'dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_MetodoPago 
        ON dbo.Encomiendas (MetodoPago) 
        INCLUDE (CostoCarga, RecargoDelivery, FechaRecepcion, CajaTurnoID);
    END;

    -- 3. Asignar 'Efectivo' a registros previos que no tengan método de pago registrado
    UPDATE dbo.Boletos 
    SET MetodoPago = 'Efectivo' 
    WHERE MetodoPago IS NULL;

    UPDATE dbo.Encomiendas 
    SET MetodoPago = 'Efectivo' 
    WHERE MetodoPago IS NULL;

    -- 4. Restricciones de chequeo para canales de pago permitidos en el sistema
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CHK_Boletos_MetodoPago' AND parent_object_id = OBJECT_ID(N'dbo.Boletos'))
    BEGIN
        ALTER TABLE dbo.Boletos DROP CONSTRAINT CHK_Boletos_MetodoPago;
    END;

    ALTER TABLE dbo.Boletos ADD CONSTRAINT CHK_Boletos_MetodoPago 
    CHECK (MetodoPago IS NULL OR MetodoPago IN (
        'Efectivo', 'Yape', 'Plin', 'Transferencia BCP', 'Transferencia BBVA', 'Tarjeta de Débito', 'Tarjeta de Crédito'
    ));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CHK_Encomiendas_MetodoPago' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas'))
    BEGIN
        ALTER TABLE dbo.Encomiendas DROP CONSTRAINT CHK_Encomiendas_MetodoPago;
    END;

    ALTER TABLE dbo.Encomiendas ADD CONSTRAINT CHK_Encomiendas_MetodoPago 
    CHECK (MetodoPago IS NULL OR MetodoPago IN (
        'Efectivo', 'Yape', 'Plin', 'Transferencia BCP', 'Transferencia BBVA', 'Tarjeta de Débito', 'Tarjeta de Crédito'
    ));

    COMMIT TRANSACTION;
    PRINT 'Métodos de pago y operaciones digitales agregados correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
