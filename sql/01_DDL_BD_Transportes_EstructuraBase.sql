-- Script 01: Creación de base de datos y tablas principales

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'BD_Transportes')
BEGIN
    CREATE DATABASE [BD_Transportes];
END
GO

USE [BD_Transportes];
GO

SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Tabla dbo.Usuarios: Almacena los operadores de ventanilla y administradores que acceden al sistema
    IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Usuarios (
            UsuarioID INT IDENTITY(1,1) NOT NULL,
            Username NVARCHAR(40) NOT NULL,
            PasswordHash NVARCHAR(256) NOT NULL,
            Rol NVARCHAR(20) NOT NULL,
            Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
            Nombres NVARCHAR(100) NULL,
            CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (UsuarioID),
            CONSTRAINT UQ_Usuarios_Username UNIQUE NONCLUSTERED (Username),
            CONSTRAINT CHK_Usuarios_Rol CHECK (Rol IN ('Operador', 'Administrador', 'Supervisor', 'Cajero'))
        );
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'Nombres')
        BEGIN
            ALTER TABLE dbo.Usuarios ADD Nombres NVARCHAR(100) NULL;
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Rol' AND object_id = OBJECT_ID('dbo.Usuarios'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Usuarios_Rol ON dbo.Usuarios (Rol) INCLUDE (Username, Activo, Nombres);
    END;

    -- 2. Tabla dbo.CajasTurno: Control de turnos de caja por operador (monto inicial de apertura, ingresos por ventas y estado)
    IF OBJECT_ID('dbo.CajasTurno', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CajasTurno (
            CajaTurnoID INT IDENTITY(1,1) NOT NULL,
            UsuarioID INT NOT NULL,
            MontoApertura DECIMAL(18,2) NOT NULL,
            MontoActual DECIMAL(18,2) NOT NULL,
            FechaApertura DATETIME NOT NULL CONSTRAINT DF_CajasTurno_FechaApertura DEFAULT (GETDATE()),
            Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_CajasTurno_Estado DEFAULT ('Abierta'),
            CONSTRAINT PK_CajasTurno PRIMARY KEY CLUSTERED (CajaTurnoID),
            CONSTRAINT FK_CajasTurno_Usuarios FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuarios (UsuarioID),
            CONSTRAINT CHK_CajasTurno_Estado CHECK (Estado IN ('Abierta', 'Abierto', 'Cerrada', 'Cerrado')),
            CONSTRAINT CHK_CajasTurno_Montos CHECK (MontoApertura >= 0 AND MontoActual >= 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CajasTurno_UsuarioID' AND object_id = OBJECT_ID('dbo.CajasTurno'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_CajasTurno_UsuarioID ON dbo.CajasTurno (UsuarioID, Estado) INCLUDE (MontoActual, FechaApertura);
    END;

    -- 3. Tabla dbo.Buses: Registro de las unidades vehiculares de la empresa (placa y capacidad máxima de pasajeros)
    IF OBJECT_ID('dbo.Buses', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Buses (
            BusID INT IDENTITY(1,1) NOT NULL,
            Placa NVARCHAR(10) NOT NULL,
            Capacidad INT NOT NULL,
            CONSTRAINT PK_Buses PRIMARY KEY CLUSTERED (BusID),
            CONSTRAINT UQ_Buses_Placa UNIQUE NONCLUSTERED (Placa),
            CONSTRAINT CHK_Buses_Capacidad CHECK (Capacidad > 0 AND Capacidad <= 100)
        );
    END;

    -- 4. Tabla dbo.Asientos: Catálogo físico de asientos por bus, distribuidos entre el primer y segundo piso
    IF OBJECT_ID('dbo.Asientos', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Asientos (
            AsientoID INT IDENTITY(1,1) NOT NULL,
            BusID INT NOT NULL,
            NroAsiento INT NOT NULL,
            Piso INT NOT NULL CONSTRAINT DF_Asientos_Piso DEFAULT (1),
            CONSTRAINT PK_Asientos PRIMARY KEY CLUSTERED (AsientoID),
            CONSTRAINT FK_Asientos_Buses FOREIGN KEY (BusID) REFERENCES dbo.Buses (BusID),
            CONSTRAINT UQ_Asientos_Bus_Nro UNIQUE NONCLUSTERED (BusID, NroAsiento),
            CONSTRAINT CHK_Asientos_NroAsiento CHECK (NroAsiento >= 1 AND NroAsiento <= 100),
            CONSTRAINT CHK_Asientos_Piso CHECK (Piso IN (1, 2))
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Asientos_BusID' AND object_id = OBJECT_ID('dbo.Asientos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Asientos_BusID ON dbo.Asientos (BusID) INCLUDE (NroAsiento, Piso);
    END;

    -- 5. Tabla dbo.Viajes: Programación de itinerarios de salida (origen, destino, fecha, horario, categoría y precio base)
    IF OBJECT_ID('dbo.Viajes', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Viajes (
            ViajeID INT IDENTITY(1,1) NOT NULL,
            BusID INT NOT NULL,
            Origen NVARCHAR(50) NOT NULL,
            Destino NVARCHAR(50) NOT NULL,
            TipoServicio NVARCHAR(50) NOT NULL CONSTRAINT DF_Viajes_TipoServicio DEFAULT ('Directo'),
            FechaSalida DATETIME NOT NULL,
            FechaHoraLlegada DATETIME NULL,
            Categoria NVARCHAR(50) NOT NULL CONSTRAINT DF_Viajes_Categoria DEFAULT (N'Clásico'),
            DuracionEstimada NVARCHAR(30) NULL,
            PrecioBase DECIMAL(18,2) NOT NULL,
            CONSTRAINT PK_Viajes PRIMARY KEY CLUSTERED (ViajeID),
            CONSTRAINT FK_Viajes_Buses FOREIGN KEY (BusID) REFERENCES dbo.Buses (BusID),
            CONSTRAINT CHK_Viajes_PrecioBase CHECK (PrecioBase >= 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Viajes_BusID' AND object_id = OBJECT_ID('dbo.Viajes'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Viajes_BusID ON dbo.Viajes (BusID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Viajes_Origen_Destino_FechaSalida' AND object_id = OBJECT_ID('dbo.Viajes'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Viajes_Origen_Destino_FechaSalida 
        ON dbo.Viajes (Origen, Destino, FechaSalida) 
        INCLUDE (PrecioBase, Categoria, TipoServicio, DuracionEstimada, FechaHoraLlegada);
    END;

    -- 6. Tabla dbo.EstadoAsientosViaje: Control de disponibilidad de asientos por viaje (Libre/Ocupado) y control de concurrencia
    IF OBJECT_ID('dbo.EstadoAsientosViaje', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadoAsientosViaje (
            EstadoAsientoID INT IDENTITY(1,1) NOT NULL,
            ViajeID INT NOT NULL,
            NroAsiento INT NOT NULL,
            Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_EstadoAsientosViaje_Estado DEFAULT ('Libre'),
            RowVersion ROWVERSION NOT NULL,
            CONSTRAINT PK_EstadoAsientosViaje PRIMARY KEY CLUSTERED (EstadoAsientoID),
            CONSTRAINT FK_EstadoAsientosViaje_Viajes FOREIGN KEY (ViajeID) REFERENCES dbo.Viajes (ViajeID),
            CONSTRAINT UQ_EstadoAsientosViaje_Viaje_Nro UNIQUE NONCLUSTERED (ViajeID, NroAsiento),
            CONSTRAINT CHK_EstadoAsientosViaje_Estado CHECK (Estado IN ('Libre', 'Reservado', 'Ocupado'))
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EstadoAsientosViaje_ViajeID' AND object_id = OBJECT_ID('dbo.EstadoAsientosViaje'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_EstadoAsientosViaje_ViajeID 
        ON dbo.EstadoAsientosViaje (ViajeID) 
        INCLUDE (NroAsiento, Estado);
    END;

    -- 7. Tabla dbo.Boletos: Registro de boletos de viaje emitidos a pasajeros vinculados al turno de caja activo
    IF OBJECT_ID('dbo.Boletos', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Boletos (
            BoletoID INT IDENTITY(1,1) NOT NULL,
            ViajeID INT NOT NULL,
            NroAsiento INT NOT NULL,
            DniPasajero NVARCHAR(8) NOT NULL,
            NombrePasajero NVARCHAR(100) NOT NULL,
            PrecioFinal DECIMAL(18,2) NOT NULL,
            FechaEmision DATETIME NOT NULL CONSTRAINT DF_Boletos_FechaEmision DEFAULT (GETDATE()),
            CajaTurnoID INT NOT NULL,
            MetodoPago VARCHAR(30) NULL,
            NumeroOperacion VARCHAR(50) NULL,
            CONSTRAINT PK_Boletos PRIMARY KEY CLUSTERED (BoletoID),
            CONSTRAINT FK_Boletos_Viajes FOREIGN KEY (ViajeID) REFERENCES dbo.Viajes (ViajeID),
            CONSTRAINT FK_Boletos_CajasTurno FOREIGN KEY (CajaTurnoID) REFERENCES dbo.CajasTurno (CajaTurnoID),
            CONSTRAINT CHK_Boletos_PrecioFinal CHECK (PrecioFinal >= 0),
            CONSTRAINT CHK_Boletos_DniPasajero CHECK (LEN(DniPasajero) = 8)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Boletos_ViajeID' AND object_id = OBJECT_ID('dbo.Boletos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Boletos_ViajeID ON dbo.Boletos (ViajeID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Boletos_CajaTurnoID' AND object_id = OBJECT_ID('dbo.Boletos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Boletos_CajaTurnoID ON dbo.Boletos (CajaTurnoID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Boletos_ViajeID_NroAsiento' AND object_id = OBJECT_ID('dbo.Boletos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Boletos_ViajeID_NroAsiento ON dbo.Boletos (ViajeID, NroAsiento);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Boletos_DniPasajero' AND object_id = OBJECT_ID('dbo.Boletos'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Boletos_DniPasajero ON dbo.Boletos (DniPasajero);
    END;

    -- 8. Tabla dbo.Encomiendas: Registro y despacho de carga en bodega (datos de remitente, destinatario, peso y costo de envío)
    IF OBJECT_ID('dbo.Encomiendas', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Encomiendas (
            EncomiendaID INT IDENTITY(1,1) NOT NULL,
            BoletoID INT NULL,
            ViajeID INT NULL,
            CajaTurnoID INT NULL,
            Descripcion NVARCHAR(150) NOT NULL,
            PesoKg DECIMAL(10,2) NOT NULL,
            CostoCarga DECIMAL(18,2) NOT NULL,
            FechaRecepcion DATETIME NOT NULL CONSTRAINT DF_Encomiendas_FechaRecepcion DEFAULT (GETDATE()),
            RemitenteTipoDoc VARCHAR(10) NULL,
            RemitenteDoc VARCHAR(15) NULL,
            RemitenteNombre NVARCHAR(120) NULL,
            RemitenteTelefono VARCHAR(15) NULL,
            DestinatarioTipoDoc VARCHAR(10) NULL,
            DestinatarioDoc VARCHAR(15) NULL,
            DestinatarioNombre NVARCHAR(120) NULL,
            DestinatarioTelefono VARCHAR(15) NULL,
            ModalidadEntrega VARCHAR(30) NULL CONSTRAINT DF_Encomiendas_ModalidadEntrega DEFAULT ('Agencia'),
            DireccionEntrega NVARCHAR(200) NULL,
            RecargoDelivery DECIMAL(10,2) NULL,
            MetodoPago VARCHAR(30) NULL,
            NumeroOperacion VARCHAR(50) NULL,
            CONSTRAINT PK_Encomiendas PRIMARY KEY CLUSTERED (EncomiendaID),
            CONSTRAINT FK_Encomiendas_Boletos FOREIGN KEY (BoletoID) REFERENCES dbo.Boletos (BoletoID),
            CONSTRAINT FK_Encomiendas_Viajes FOREIGN KEY (ViajeID) REFERENCES dbo.Viajes (ViajeID),
            CONSTRAINT FK_Encomiendas_CajasTurno FOREIGN KEY (CajaTurnoID) REFERENCES dbo.CajasTurno (CajaTurnoID),
            CONSTRAINT CHK_Encomiendas_PesoKg CHECK (PesoKg > 0),
            CONSTRAINT CHK_Encomiendas_CostoCarga CHECK (CostoCarga >= 0),
            CONSTRAINT CHK_Encomiendas_ModalidadEntrega CHECK (ModalidadEntrega IS NULL OR ModalidadEntrega IN ('Agencia', 'Domicilio'))
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Encomiendas_BoletoID' AND object_id = OBJECT_ID('dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_BoletoID ON dbo.Encomiendas (BoletoID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Encomiendas_ViajeID' AND object_id = OBJECT_ID('dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_ViajeID ON dbo.Encomiendas (ViajeID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Encomiendas_CajaTurnoID' AND object_id = OBJECT_ID('dbo.Encomiendas'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Encomiendas_CajaTurnoID ON dbo.Encomiendas (CajaTurnoID);
    END;

    COMMIT TRANSACTION;
    PRINT 'Tablas e índices creados correctamente.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMsg, 16, 1);
END CATCH;
GO
