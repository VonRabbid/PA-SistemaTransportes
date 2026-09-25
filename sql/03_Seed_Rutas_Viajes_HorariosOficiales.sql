-- Script 03: Registro de buses, catálogo de asientos y matriz de viajes

USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Registro de buses interprovinciales disponibles en la flota
    IF NOT EXISTS (SELECT 1 FROM dbo.Buses WHERE BusID = 1)
    BEGIN
        SET IDENTITY_INSERT dbo.Buses ON;
        INSERT INTO dbo.Buses (BusID, Placa, Capacidad) VALUES (1, N'B1A-982', 40);
        SET IDENTITY_INSERT dbo.Buses OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Buses WHERE BusID = 2)
    BEGIN
        SET IDENTITY_INSERT dbo.Buses ON;
        INSERT INTO dbo.Buses (BusID, Placa, Capacidad) VALUES (2, N'T2B-451', 40);
        SET IDENTITY_INSERT dbo.Buses OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Buses WHERE BusID = 3)
    BEGIN
        SET IDENTITY_INSERT dbo.Buses ON;
        INSERT INTO dbo.Buses (BusID, Placa, Capacidad) VALUES (3, N'C3C-890', 40);
        SET IDENTITY_INSERT dbo.Buses OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Buses WHERE BusID = 4)
    BEGIN
        SET IDENTITY_INSERT dbo.Buses ON;
        INSERT INTO dbo.Buses (BusID, Placa, Capacidad) VALUES (4, N'P5P-202', 40);
        SET IDENTITY_INSERT dbo.Buses OFF;
    END;

    -- 2. Generación automática de los 40 asientos físicos por cada bus (asientos 1-20 en Piso 1 y 21-40 en Piso 2)
    ;WITH Numeros40 AS (
        SELECT 1 AS NroAsiento
        UNION ALL SELECT 2  UNION ALL SELECT 3  UNION ALL SELECT 4  UNION ALL SELECT 5
        UNION ALL SELECT 6  UNION ALL SELECT 7  UNION ALL SELECT 8  UNION ALL SELECT 9  UNION ALL SELECT 10
        UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
        UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
        UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30
        UNION ALL SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35
        UNION ALL SELECT 36 UNION ALL SELECT 37 UNION ALL SELECT 38 UNION ALL SELECT 39 UNION ALL SELECT 40
    )
    INSERT INTO dbo.Asientos (BusID, NroAsiento, Piso)
    SELECT 
        b.BusID,
        n.NroAsiento,
        CASE WHEN n.NroAsiento <= 20 THEN 1 ELSE 2 END AS Piso
    FROM dbo.Buses b
    CROSS JOIN Numeros40 n
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Asientos a
        WHERE a.BusID = b.BusID AND a.NroAsiento = n.NroAsiento
    );

    -- Limpieza de tablas dependientes para reiniciar la programación
    DELETE FROM dbo.Encomiendas;
    DELETE FROM dbo.Boletos;
    DELETE FROM dbo.EstadoAsientosViaje;
    DELETE FROM dbo.Viajes;
    DBCC CHECKIDENT ('dbo.Viajes', RESEED, 0);

    -- 3. Catálogo de rutas interprovinciales con distancias y tiempos estimados de viaje
    CREATE TABLE #RutasBase (
        RutaID INT IDENTITY(1,1),
        Origen NVARCHAR(50) NOT NULL,
        Destino NVARCHAR(50) NOT NULL,
        TipoServicio NVARCHAR(50) NOT NULL,
        PrecioBase DECIMAL(18,2) NOT NULL,
        DuracionMinutos INT NOT NULL
    );

    -- * Cajamarca
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Cajamarca', N'Chilete', N'Directo', 25.00, 150),
    (N'Cajamarca', N'Ciudad de Dios', N'Directo', 35.00, 210),
    (N'Cajamarca', N'Chepén', N'Directo', 40.00, 270),
    (N'Cajamarca', N'Trujillo', N'Directo', 50.00, 390),
    (N'Cajamarca', N'Chiclayo', N'Directo', 45.00, 360),
    (N'Cajamarca', N'Chimbote', N'Directo', 65.00, 480),
    (N'Cajamarca', N'Lima', N'Directo', 95.00, 870),
    (N'Cajamarca', N'Jaén', N'Directo', 55.00, 420),
    (N'Cajamarca', N'Nueva Cajamarca', N'Conexión (vía Chiclayo)', 110.00, 690),
    (N'Cajamarca', N'Moyobamba', N'Conexión (vía Chiclayo)', 120.00, 750),
    (N'Cajamarca', N'Tarapoto', N'Conexión (vía Chiclayo)', 130.00, 840);

    -- * Tarapoto
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Tarapoto', N'Moyobamba', N'Directo', 30.00, 120),
    (N'Tarapoto', N'Nueva Cajamarca', N'Directo', 40.00, 180),
    (N'Tarapoto', N'Jaén', N'Directo', 80.00, 480),
    (N'Tarapoto', N'Chiclayo', N'Directo', 100.00, 660),
    (N'Tarapoto', N'Chepén', N'Directo', 110.00, 720),
    (N'Tarapoto', N'Ciudad de Dios', N'Directo', 115.00, 750),
    (N'Tarapoto', N'Trujillo', N'Directo', 120.00, 810),
    (N'Tarapoto', N'Chimbote', N'Directo', 130.00, 930),
    (N'Tarapoto', N'Lima', N'Directo', 140.00, 1320),
    (N'Tarapoto', N'Chilete', N'Conexión (vía Chiclayo)', 125.00, 870),
    (N'Tarapoto', N'Cajamarca', N'Conexión (vía Chiclayo)', 130.00, 840);

    -- * Moyobamba
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Moyobamba', N'Tarapoto', N'Directo', 30.00, 120),
    (N'Moyobamba', N'Nueva Cajamarca', N'Directo', 20.00, 60),
    (N'Moyobamba', N'Jaén', N'Directo', 70.00, 420),
    (N'Moyobamba', N'Chiclayo', N'Directo', 90.00, 600),
    (N'Moyobamba', N'Chepén', N'Directo', 100.00, 660),
    (N'Moyobamba', N'Ciudad de Dios', N'Directo', 105.00, 690),
    (N'Moyobamba', N'Trujillo', N'Directo', 110.00, 750),
    (N'Moyobamba', N'Chimbote', N'Directo', 120.00, 870),
    (N'Moyobamba', N'Lima', N'Directo', 130.00, 1260),
    (N'Moyobamba', N'Chilete', N'Conexión (vía Chiclayo)', 115.00, 810),
    (N'Moyobamba', N'Cajamarca', N'Conexión (vía Chiclayo)', 120.00, 780);

    -- * Nueva Cajamarca
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Nueva Cajamarca', N'Tarapoto', N'Directo', 40.00, 180),
    (N'Nueva Cajamarca', N'Moyobamba', N'Directo', 20.00, 60),
    (N'Nueva Cajamarca', N'Jaén', N'Directo', 60.00, 360),
    (N'Nueva Cajamarca', N'Chiclayo', N'Directo', 80.00, 540),
    (N'Nueva Cajamarca', N'Chepén', N'Directo', 90.00, 600),
    (N'Nueva Cajamarca', N'Ciudad de Dios', N'Directo', 95.00, 630),
    (N'Nueva Cajamarca', N'Trujillo', N'Directo', 100.00, 690),
    (N'Nueva Cajamarca', N'Chimbote', N'Directo', 110.00, 810),
    (N'Nueva Cajamarca', N'Lima', N'Directo', 120.00, 1200),
    (N'Nueva Cajamarca', N'Chilete', N'Conexión (vía Chiclayo)', 105.00, 750),
    (N'Nueva Cajamarca', N'Cajamarca', N'Conexión (vía Chiclayo)', 110.00, 720);

    -- * Chiclayo
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Chiclayo', N'Chepén', N'Directo', 15.00, 75),
    (N'Chiclayo', N'Ciudad de Dios', N'Directo', 20.00, 105),
    (N'Chiclayo', N'Chilete', N'Directo', 35.00, 210),
    (N'Chiclayo', N'Cajamarca', N'Directo', 45.00, 360),
    (N'Chiclayo', N'Trujillo', N'Directo', 30.00, 180),
    (N'Chiclayo', N'Chimbote', N'Directo', 50.00, 330),
    (N'Chiclayo', N'Lima', N'Directo', 80.00, 720),
    (N'Chiclayo', N'Jaén', N'Directo', 45.00, 360),
    (N'Chiclayo', N'Nueva Cajamarca', N'Directo', 80.00, 540),
    (N'Chiclayo', N'Moyobamba', N'Directo', 90.00, 600),
    (N'Chiclayo', N'Tarapoto', N'Directo', 100.00, 660);

    -- * Chepén
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Chepén', N'Ciudad de Dios', N'Directo', 10.00, 30),
    (N'Chepén', N'Chilete', N'Directo', 25.00, 150),
    (N'Chepén', N'Cajamarca', N'Directo', 40.00, 270),
    (N'Chepén', N'Chiclayo', N'Directo', 15.00, 75),
    (N'Chepén', N'Trujillo', N'Directo', 25.00, 120),
    (N'Chepén', N'Chimbote', N'Directo', 45.00, 240),
    (N'Chepén', N'Lima', N'Directo', 75.00, 660),
    (N'Chepén', N'Jaén', N'Directo', 55.00, 420),
    (N'Chepén', N'Nueva Cajamarca', N'Directo', 90.00, 600),
    (N'Chepén', N'Moyobamba', N'Directo', 100.00, 660),
    (N'Chepén', N'Tarapoto', N'Directo', 110.00, 720);

    -- * Ciudad de Dios
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Ciudad de Dios', N'Chilete', N'Directo', 18.00, 105),
    (N'Ciudad de Dios', N'Cajamarca', N'Directo', 35.00, 210),
    (N'Ciudad de Dios', N'Chepén', N'Directo', 10.00, 30),
    (N'Ciudad de Dios', N'Chiclayo', N'Directo', 20.00, 105),
    (N'Ciudad de Dios', N'Trujillo', N'Directo', 28.00, 135),
    (N'Ciudad de Dios', N'Chimbote', N'Directo', 48.00, 255),
    (N'Ciudad de Dios', N'Lima', N'Directo', 78.00, 675),
    (N'Ciudad de Dios', N'Jaén', N'Directo', 58.00, 435),
    (N'Ciudad de Dios', N'Nueva Cajamarca', N'Directo', 95.00, 615),
    (N'Ciudad de Dios', N'Moyobamba', N'Directo', 105.00, 675),
    (N'Ciudad de Dios', N'Tarapoto', N'Directo', 115.00, 735);

    -- * Chilete
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Chilete', N'Cajamarca', N'Directo', 25.00, 150),
    (N'Chilete', N'Ciudad de Dios', N'Directo', 18.00, 105),
    (N'Chilete', N'Chepén', N'Directo', 25.00, 150),
    (N'Chilete', N'Chiclayo', N'Directo', 35.00, 210),
    (N'Chilete', N'Trujillo', N'Directo', 35.00, 210),
    (N'Chilete', N'Chimbote', N'Directo', 55.00, 330),
    (N'Chilete', N'Lima', N'Directo', 85.00, 750),
    (N'Chilete', N'Jaén', N'Directo', 65.00, 480),
    (N'Chilete', N'Nueva Cajamarca', N'Conexión (vía Chiclayo)', 105.00, 720),
    (N'Chilete', N'Moyobamba', N'Conexión (vía Chiclayo)', 115.00, 780),
    (N'Chilete', N'Tarapoto', N'Conexión (vía Chiclayo)', 125.00, 840);

    -- * Trujillo
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Trujillo', N'Chimbote', N'Directo', 20.00, 120),
    (N'Trujillo', N'Lima', N'Directo', 60.00, 540),
    (N'Trujillo', N'Chepén', N'Directo', 25.00, 120),
    (N'Trujillo', N'Ciudad de Dios', N'Directo', 28.00, 135),
    (N'Trujillo', N'Chilete', N'Directo', 35.00, 210),
    (N'Trujillo', N'Cajamarca', N'Directo', 50.00, 390),
    (N'Trujillo', N'Chiclayo', N'Directo', 30.00, 180),
    (N'Trujillo', N'Jaén', N'Directo', 70.00, 510),
    (N'Trujillo', N'Nueva Cajamarca', N'Directo', 100.00, 690),
    (N'Trujillo', N'Moyobamba', N'Directo', 110.00, 750),
    (N'Trujillo', N'Tarapoto', N'Directo', 120.00, 810);

    -- * Chimbote
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Chimbote', N'Trujillo', N'Directo', 20.00, 120),
    (N'Chimbote', N'Lima', N'Directo', 50.00, 420),
    (N'Chimbote', N'Chepén', N'Directo', 45.00, 240),
    (N'Chimbote', N'Ciudad de Dios', N'Directo', 48.00, 255),
    (N'Chimbote', N'Chilete', N'Directo', 55.00, 330),
    (N'Chimbote', N'Cajamarca', N'Directo', 65.00, 480),
    (N'Chimbote', N'Chiclayo', N'Directo', 50.00, 330),
    (N'Chimbote', N'Jaén', N'Directo', 85.00, 630),
    (N'Chimbote', N'Nueva Cajamarca', N'Directo', 110.00, 810),
    (N'Chimbote', N'Moyobamba', N'Directo', 120.00, 870),
    (N'Chimbote', N'Tarapoto', N'Directo', 130.00, 930);

    -- * Lima
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Lima', N'Chimbote', N'Directo', 50.00, 420),
    (N'Lima', N'Trujillo', N'Directo', 60.00, 540),
    (N'Lima', N'Chepén', N'Directo', 75.00, 660),
    (N'Lima', N'Ciudad de Dios', N'Directo', 78.00, 675),
    (N'Lima', N'Chilete', N'Directo', 85.00, 750),
    (N'Lima', N'Cajamarca', N'Directo', 95.00, 870),
    (N'Lima', N'Chiclayo', N'Directo', 80.00, 720),
    (N'Lima', N'Jaén', N'Directo', 110.00, 990),
    (N'Lima', N'Nueva Cajamarca', N'Directo', 120.00, 1200),
    (N'Lima', N'Moyobamba', N'Directo', 130.00, 1260),
    (N'Lima', N'Tarapoto', N'Directo', 140.00, 1320);

    -- * Jaén
    INSERT INTO #RutasBase (Origen, Destino, TipoServicio, PrecioBase, DuracionMinutos) VALUES
    (N'Jaén', N'Tarapoto', N'Directo', 80.00, 480),
    (N'Jaén', N'Moyobamba', N'Directo', 70.00, 420),
    (N'Jaén', N'Nueva Cajamarca', N'Directo', 60.00, 360),
    (N'Jaén', N'Chiclayo', N'Directo', 45.00, 360),
    (N'Jaén', N'Chepén', N'Directo', 55.00, 420),
    (N'Jaén', N'Ciudad de Dios', N'Directo', 58.00, 435),
    (N'Jaén', N'Trujillo', N'Directo', 70.00, 510),
    (N'Jaén', N'Chimbote', N'Directo', 85.00, 630),
    (N'Jaén', N'Lima', N'Directo', 110.00, 990),
    (N'Jaén', N'Cajamarca', N'Directo', 55.00, 420),
    (N'Jaén', N'Chilete', N'Conexión (vía Chiclayo)', 70.00, 480);

    -- 4. Programación de salidas diarias en distintos turnos (mañana, tarde y noche) con tarifas por categoría
    CREATE TABLE #EsquemaHorarios (
        TurnoID INT,
        Hora INT,
        Minuto INT,
        Categoria NVARCHAR(50),
        IncrementoPrecio DECIMAL(18,2)
    );

    INSERT INTO #EsquemaHorarios (TurnoID, Hora, Minuto, Categoria, IncrementoPrecio) VALUES
    (1,  6, 30, N'Clásico',        0.00),
    (2, 13, 45, N'Ejecutivo',      15.00),
    (3, 19, 30, N'VIP',            30.00),
    (4, 22, 15, N'Premium',        50.00);

    DECLARE @FechaBase DATE = CAST(GETDATE() AS DATE);

    -- Inserción de viajes programados
    INSERT INTO dbo.Viajes (
        BusID, Origen, Destino, TipoServicio, 
        FechaSalida, FechaHoraLlegada, Categoria, PrecioBase, DuracionEstimada
    )
    SELECT 
        CASE ((r.RutaID + h.TurnoID) % 4)
            WHEN 0 THEN 1
            WHEN 1 THEN 2
            WHEN 2 THEN 3
            ELSE 4
        END AS BusID,
        r.Origen,
        r.Destino,
        r.TipoServicio,
        DATEADD(MINUTE, h.Minuto, DATEADD(HOUR, h.Hora, CAST(@FechaBase AS DATETIME))) AS FechaSalida,
        DATEADD(MINUTE, r.DuracionMinutos, DATEADD(MINUTE, h.Minuto, DATEADD(HOUR, h.Hora, CAST(@FechaBase AS DATETIME)))) AS FechaHoraLlegada,
        h.Categoria,
        (r.PrecioBase + h.IncrementoPrecio) AS PrecioBase,
        CONCAT(RIGHT('0' + CAST(r.DuracionMinutos / 60 AS VARCHAR(5)), 2), ':', RIGHT('0' + CAST(r.DuracionMinutos % 60 AS VARCHAR(5)), 2), ' hrs') AS DuracionEstimada
    FROM #RutasBase r
    CROSS JOIN #EsquemaHorarios h
    WHERE 
        (h.TurnoID <= 3) 
        OR (h.TurnoID = 4 AND (r.Origen IN (N'Lima', N'Trujillo', N'Cajamarca', N'Chiclayo', N'Tarapoto') 
                            OR r.Destino IN (N'Lima', N'Trujillo', N'Cajamarca', N'Chiclayo', N'Tarapoto')))
    ORDER BY r.RutaID, h.Hora;

    -- 5. Inicialización de los 40 estados de asiento en condición 'Libre' para cada viaje programado
    ;WITH Numeros40 AS (
        SELECT 1 AS NroAsiento
        UNION ALL SELECT 2  UNION ALL SELECT 3  UNION ALL SELECT 4  UNION ALL SELECT 5
        UNION ALL SELECT 6  UNION ALL SELECT 7  UNION ALL SELECT 8  UNION ALL SELECT 9  UNION ALL SELECT 10
        UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
        UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
        UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30
        UNION ALL SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35
        UNION ALL SELECT 36 UNION ALL SELECT 37 UNION ALL SELECT 38 UNION ALL SELECT 39 UNION ALL SELECT 40
    )
    INSERT INTO dbo.EstadoAsientosViaje (ViajeID, NroAsiento, Estado)
    SELECT 
        v.ViajeID,
        n.NroAsiento,
        N'Libre' AS Estado
    FROM dbo.Viajes v
    CROSS JOIN Numeros40 n;

    -- Registro de boletos iniciales para demostración en el primer viaje
    DECLARE @PrimerViajeID INT = (SELECT MIN(ViajeID) FROM dbo.Viajes);
    DECLARE @CajaID INT = (SELECT TOP 1 CajaTurnoID FROM dbo.CajasTurno WHERE Estado IN (N'Abierto', N'Abierta') ORDER BY CajaTurnoID ASC);
    DECLARE @PrecioPrimerViaje DECIMAL(18,2) = (SELECT PrecioBase FROM dbo.Viajes WHERE ViajeID = @PrimerViajeID);

    IF @CajaID IS NOT NULL AND @PrimerViajeID IS NOT NULL
    BEGIN
        UPDATE dbo.EstadoAsientosViaje
        SET Estado = N'Ocupado'
        WHERE ViajeID = @PrimerViajeID AND NroAsiento IN (4, 7);

        INSERT INTO dbo.Boletos (ViajeID, NroAsiento, DniPasajero, NombrePasajero, PrecioFinal, FechaEmision, CajaTurnoID, MetodoPago, NumeroOperacion)
        VALUES 
        (@PrimerViajeID, 4, N'45892147', N'Carlos Mendoza Paredes', @PrecioPrimerViaje, DATEADD(HOUR, -2, GETDATE()), @CajaID, 'Efectivo', NULL),
        (@PrimerViajeID, 7, N'78954123', N'Lucía Fernández Silva',  @PrecioPrimerViaje, DATEADD(HOUR, -1, GETDATE()), @CajaID, 'Efectivo', NULL);

        UPDATE dbo.CajasTurno
        SET MontoActual = MontoApertura + (@PrecioPrimerViaje * 2)
        WHERE CajaTurnoID = @CajaID;
    END;

    DROP TABLE #EsquemaHorarios;
    DROP TABLE #RutasBase;

    COMMIT TRANSACTION;
    PRINT 'Buses, rutas, viajes y asientos registrados correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    IF OBJECT_ID('tempdb..#EsquemaHorarios') IS NOT NULL DROP TABLE #EsquemaHorarios;
    IF OBJECT_ID('tempdb..#RutasBase') IS NOT NULL DROP TABLE #RutasBase;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
