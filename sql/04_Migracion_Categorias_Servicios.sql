-- Script 04: Migración y normalización de categorías de servicio
-- Base de datos: BD_Transportes
-- Estandarización de niveles de servicio en viajes: Clásico, Ejecutivo, VIP y Premium con sus respectivas restricciones de validación

USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Asegurar columna Categoria en dbo.Viajes
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'dbo.Viajes') AND name = N'Categoria'
    )
    BEGIN
        ALTER TABLE dbo.Viajes ADD Categoria NVARCHAR(50) NULL;
    END;

    -- 2. Migración escalonada de valores previos para evitar conflictos de nombres
    -- Plus -> Premium (Servicio de mayor confort)
    UPDATE dbo.Viajes 
    SET Categoria = N'Premium' 
    WHERE Categoria = N'Plus';

    -- Oro -> VIP (Servicio de alta gama)
    UPDATE dbo.Viajes 
    SET Categoria = N'VIP' 
    WHERE Categoria = N'Oro';

    -- Antiguo Ejecutivo -> Clásico (Servicio base económico)
    UPDATE dbo.Viajes 
    SET Categoria = N'Clásico' 
    WHERE Categoria = N'Ejecutivo';

    -- Plata -> Ejecutivo (Servicio intermedio)
    UPDATE dbo.Viajes 
    SET Categoria = N'Ejecutivo' 
    WHERE Categoria = N'Plata';

    -- Normalizar registros nulos o no clasificados hacia el servicio Clásico
    UPDATE dbo.Viajes
    SET Categoria = N'Clásico'
    WHERE Categoria IS NULL 
       OR Categoria NOT IN (N'Clásico', N'Ejecutivo', N'VIP', N'Premium');

    -- 3. Actualizar columna desnormalizada en Boletos (si existiera en versiones previas)
    IF COL_LENGTH('dbo.Boletos', 'Categoria') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE dbo.Boletos SET Categoria = N''Premium'' WHERE Categoria = N''Plus'';
            UPDATE dbo.Boletos SET Categoria = N''VIP'' WHERE Categoria = N''Oro'';
            UPDATE dbo.Boletos SET Categoria = N''Clásico'' WHERE Categoria = N''Ejecutivo'';
            UPDATE dbo.Boletos SET Categoria = N''Ejecutivo'' WHERE Categoria = N''Plata'';
            UPDATE dbo.Boletos SET Categoria = N''Clásico'' WHERE Categoria IS NULL OR Categoria NOT IN (N''Clásico'', N''Ejecutivo'', N''VIP'', N''Premium'');
        ');
    END;

    -- 4. Restricción por defecto DF_Viajes_Categoria
    DECLARE @NombreConstraintDefault NVARCHAR(128);
    SELECT @NombreConstraintDefault = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Viajes') AND c.name = N'Categoria';

    IF @NombreConstraintDefault IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.Viajes DROP CONSTRAINT ' + @NombreConstraintDefault + ';');
    END;

    ALTER TABLE dbo.Viajes 
    ADD CONSTRAINT DF_Viajes_Categoria DEFAULT (N'Clásico') FOR Categoria;

    -- 5. Restricción de chequeo CHK_Viajes_Categoria para las 4 categorías oficiales
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CHK_Viajes_Categoria' AND parent_object_id = OBJECT_ID(N'dbo.Viajes'))
    BEGIN
        ALTER TABLE dbo.Viajes DROP CONSTRAINT CHK_Viajes_Categoria;
    END;

    ALTER TABLE dbo.Viajes
    ADD CONSTRAINT CHK_Viajes_Categoria CHECK (Categoria IN (N'Clásico', N'Ejecutivo', N'VIP', N'Premium'));

    ALTER TABLE dbo.Viajes ALTER COLUMN Categoria NVARCHAR(50) NOT NULL;

    COMMIT TRANSACTION;
    PRINT 'Categorías de servicio actualizadas correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
