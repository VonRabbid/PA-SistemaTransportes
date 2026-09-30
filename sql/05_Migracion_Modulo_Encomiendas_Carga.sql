-- Script 05: Ampliación del módulo de encomiendas y guías de despacho

USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Permitir que BoletoID sea nulo para admitir encomiendas sin pasaje de viaje
    ALTER TABLE dbo.Encomiendas ALTER COLUMN BoletoID INT NULL;

    -- 2. Trazabilidad de viaje y caja en encomiendas
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Encomiendas') AND name = N'ViajeID'
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas ADD ViajeID INT NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys 
        WHERE name = N'FK_Encomiendas_Viajes' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT FK_Encomiendas_Viajes FOREIGN KEY (ViajeID) REFERENCES dbo.Viajes (ViajeID);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Encomiendas') AND name = N'CajaTurnoID'
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas ADD CajaTurnoID INT NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys 
        WHERE name = N'FK_Encomiendas_CajasTurno' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT FK_Encomiendas_CajasTurno FOREIGN KEY (CajaTurnoID) REFERENCES dbo.CajasTurno (CajaTurnoID);
    END;

    -- Incorporación de campos para la guía de despacho de encomiendas: documentos de identidad, teléfonos y dirección para entrega a domicilio
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Encomiendas') AND name = N'RemitenteTipoDoc')
    BEGIN
        ALTER TABLE dbo.Encomiendas ADD 
            RemitenteTipoDoc VARCHAR(10) NULL,
            RemitenteDoc VARCHAR(15) NULL,
            RemitenteNombre VARCHAR(120) NULL,
            RemitenteTelefono VARCHAR(15) NULL,
            DestinatarioTipoDoc VARCHAR(10) NULL,
            DestinatarioDoc VARCHAR(15) NULL,
            DestinatarioNombre VARCHAR(120) NULL,
            DestinatarioTelefono VARCHAR(15) NULL,
            ModalidadEntrega VARCHAR(30) NULL,
            DireccionEntrega VARCHAR(200) NULL,
            RecargoDelivery DECIMAL(10,2) NULL;
    END;

    -- 4. Restricciones por defecto y validaciones de datos (modalidad de entrega, peso y costo)
    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints 
        WHERE name = N'DF_Encomiendas_ModalidadEntrega' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT DF_Encomiendas_ModalidadEntrega DEFAULT ('Agencia') FOR ModalidadEntrega;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE name = N'CHK_Encomiendas_ModalidadEntrega' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT CHK_Encomiendas_ModalidadEntrega 
        CHECK (ModalidadEntrega IS NULL OR ModalidadEntrega IN ('Agencia', 'Domicilio'));
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE name = N'CHK_Encomiendas_PesoKg' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT CHK_Encomiendas_PesoKg CHECK (PesoKg > 0);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE name = N'CHK_Encomiendas_CostoCarga' AND parent_object_id = OBJECT_ID(N'dbo.Encomiendas')
    )
    BEGIN
        ALTER TABLE dbo.Encomiendas 
        ADD CONSTRAINT CHK_Encomiendas_CostoCarga CHECK (CostoCarga >= 0);
    END;

    -- 5. Creación de índices no agrupados en llaves foráneas
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Encomiendas_BoletoID' AND object_id = OBJECT_ID(N'dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_BoletoID ON dbo.Encomiendas (BoletoID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Encomiendas_ViajeID' AND object_id = OBJECT_ID(N'dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_ViajeID ON dbo.Encomiendas (ViajeID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Encomiendas_CajaTurnoID' AND object_id = OBJECT_ID(N'dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_CajaTurnoID ON dbo.Encomiendas (CajaTurnoID);
    END;

    -- 6. Sincronización de registros históricos con su viaje y caja correspondiente
    UPDATE e
    SET e.ViajeID = b.ViajeID,
        e.CajaTurnoID = ISNULL(e.CajaTurnoID, b.CajaTurnoID)
    FROM dbo.Encomiendas e
    INNER JOIN dbo.Boletos b ON e.BoletoID = b.BoletoID
    WHERE e.ViajeID IS NULL OR e.CajaTurnoID IS NULL;

    COMMIT TRANSACTION;
    PRINT 'Módulo de encomiendas y guías de despacho actualizado correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
