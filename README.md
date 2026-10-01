\# Sistema de Transportes - Módulo de Venta Integrada y Operaciones



Sistema de escritorio empresarial desarrollado en \*\*.NET 10 con WPF\*\*, fundamentado en \*\*Clean Architecture (Arquitectura Limpia)\*\*, el patrón de diseño \*\*MVVM Puro\*\* y persistencia relacional mediante \*\*ADO.NET explícito con control transaccional ACID\*\*. La plataforma resuelve la programación de salidas interprovinciales, reserva dinámica de butacas, despacho de encomiendas en bodega y arqueo de caja por turnos operativos en tiempo real.



\---



\## Integrantes del Proyecto



\* \*\*Alvarado Minchan Cristian Paul\*\*

\* \*\*Chicoma Garcia Alvaro Jesus\*\*

\* \*\*Mendez Esparza Carlos Andres\*\*

\* \*\*Saldaña Vargas Nikolas Fabiano\*\*



\---



\## 1. Arquitectura del Software (Clean Architecture)



El sistema implementa una separación en 4 proyectos independientes para garantizar la inversión de dependencias y aislar las reglas de negocio del framework y los detalles de persistencia:



```text

Solución "Proyecto" (4 proyectos .NET 10)

├── SistemaTransportes.Domain/            # Entidades de negocio, Enums, Excepciones e Interfaces de Repositorios (Cero dependencias)

├── SistemaTransportes.Application/       # Casos de uso (Servicios), DTOs y Validaciones de negocio

├── SistemaTransportes.Infrastructure/    # Implementación ADO.NET puro, SqlConnectionFactory y SqlTransaction

└── SistemaTransportes.UI/                # Interfaz gráfica WPF bajo MVVM puro, Inyección de Dependencias y Converters

```



\### Responsabilidad por Capas



\* \*\*`SistemaTransportes.Domain`:\*\* Define las entidades de negocio (`Usuario`, `CajaTurno`, `Viaje`, `Bus`, `Asiento`, `EstadoAsientoViaje`, `Boleto`, `Encomienda`), enumeradores oficiales, excepciones de dominio tipadas (`AsientoNoDisponibleException`, `CajaNoActivaException`, `VentaValidationException`) e interfaces de persistencia (`IVentaRepository`, `ICajaTurnoRepository`, `IViajeRepository`, etc.). No depende de librerías externas ni de infraestructura.

\* \*\*`SistemaTransportes.Application`:\*\* Orquesta la lógica del negocio a través de servicios de aplicación (`VentaService`, `ConsultaViajesService`, `AuthService`, `AuditoriaService`) y objetos de transferencia de datos (`DTOs`). Aplica validaciones previas a la persistencia (máximo 5 asientos por venta, validación de DNI/RUC y límite de 50 kg en bodega).

\* \*\*`SistemaTransportes.Infrastructure`:\*\* Implementa los contratos de repositorio utilizando \*\*ADO.NET puro\*\* (`Microsoft.Data.SqlClient`) mediante sentencias SQL parametrizadas. Controla las transacciones críticas con `SqlTransaction`, gestionando la apertura asíncrona de conexiones y ejecutando `Commit()` o `Rollback()` ante colisiones o excepciones.

\* \*\*`SistemaTransportes.UI`:\*\* Capa de presentación desarrollada en WPF siguiendo el patrón MVVM sin lógica de negocio en el \*code-behind\* (`.xaml.cs`). Configura el contenedor IoC mediante `Microsoft.Extensions.DependencyInjection` en `App.xaml.cs` para resolver vistas y ViewModels de manera desacoplada.



\---



\## 2. Características Principales



\### Flujo de Venta Asistida en 3 Pasos

1\. \*\*Paso 1 - Selección de Salidas:\*\* Búsqueda interactiva de rutas por ciudad de origen, destino y fecha. Permite ordenar salidas por tarifa o por horario, validando la vigencia de las salidas mediante `SalidaDisponible`.

2\. \*\*Paso 2 - Asignación de Asientos o Encomienda:\*\*  

&#x20;  \* \*Modo Pasaje:\* Croquis interactivo del bus de 2 pisos con pasillo central (asientos 1 al 20 en Piso 1 y 21 al 40 en Piso 2) y formulario nominal de pasajeros con DNI.

&#x20;  \* \*Modo Solo Encomienda:\* Despacho directo de carga en bodega sin asignar asientos de pasajeros, con selección de bultos preconfigurados o balanza manual, asignación de remitente/destinatario y modalidad de entrega (agencia o domicilio con recargo de S/. 10.00).

3\. \*\*Paso 3 - Liquidación y Medios de Pago:\*\* Consolidación de importes (boletos + carga + delivery). Admite cobros en Efectivo (con cálculo de vuelto y alertas de saldo insuficiente) y pagos digitales (Yape, Plin y Tarjetas con validación de código de operación).



\### Integridad Transaccional ACID y Concurrencia

\* \*\*Atomicidad en Venta:\*\* La inserción de boletos, el registro de guías de carga y el ajuste de saldo en caja se ejecutan dentro del mismo scope de `SqlTransaction`. Un fallo en cualquier componente cancela la operación entera mediante `Rollback()`.

\* \*\*Control de Asientos:\*\* Validación con bloqueos de fila (`UPDLOCK, ROWLOCK`) y restricción `UQ\_Boletos\_Viaje\_Asiento` para prevenir colisiones o sobreventas en accesos simultáneos.



\### Auditoría y Control de Turnos

\* \*\*Saldo Vivo de Caja:\*\* Supervisión visual permanente del turno activo (`dbo.CajasTurno`).

\* \*\*Ventana de Auditoría:\*\* Módulo de consulta con búsqueda en tiempo real de boletos emitidos y encomiendas despachadas por operador.



\---



\## 3. Stack Tecnológico



\* \*\*Lenguaje:\*\* C# (.NET 10.0 SDK)

\* \*\*Presentación:\*\* WPF (Windows Presentation Foundation)

\* \*\*Base de Datos:\*\* Microsoft SQL Server / Azure SQL Database

\* \*\*Acceso a Datos:\*\* ADO.NET (`Microsoft.Data.SqlClient` v7.1.0)

\* \*\*Inyección de Dependencias:\*\* `Microsoft.Extensions.DependencyInjection` (v10.0.12)

\* \*\*Iconografía:\*\* `FontAwesome.Sharp` (v6.6.0)



\---



\## 4. Requisitos y Puesta en Marcha



\### Requisitos Previos

\* Sistema Operativo Windows 10/11 (64-bit).

\* Visual Studio 2022 o Visual Studio Code con el SDK de \*\*.NET 10.0\*\*.

\* Servidor accesible de \*\*Microsoft SQL Server 2019+\*\* o instancia en la nube de \*\*Azure SQL\*\*.



\### Pasos de Configuración



1\. \*\*Estructura y Carga de la Base de Datos:\*\*

&#x20;  Ejecutar los scripts de la carpeta `/sql` en orden numérico correlativo:

&#x20;  \* `01\_DDL\_BD\_Transportes\_EstructuraBase.sql`

&#x20;  \* `02\_Configuracion\_Operador\_Y\_CajaInicial.sql`

&#x20;  \* `03\_Seed\_Rutas\_Viajes\_HorariosOficiales.sql`

&#x20;  \* `04\_Migracion\_Categorias\_Servicios.sql`

&#x20;  \* `05\_Migracion\_Modulo\_Encomiendas\_Carga.sql`

&#x20;  \* `06\_Migracion\_MetodosPago\_Digitales.sql`

&#x20;  \* `07\_Procedimientos\_Rollback\_Demo.sql`

&#x20;  \* `08\_Configuracion\_Concurrencia\_Rollback\_Real.sql`



2\. \*\*Configuración de Cadena de Conexión:\*\*

&#x20;  Ajustar el archivo `SistemaTransportes.UI/App.config` con la cadena correspondiente a tu entorno:

&#x20;  ```xml

&#x20;  <connectionStrings>

&#x20;    <add name="BD\_Transportes"

&#x20;         connectionString="Server=.;Database=BD\_Transportes;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"

&#x20;         providerName="Microsoft.Data.SqlClient" />

&#x20;  </connectionStrings>

&#x20;  ```



3\. \*\*Compilación y Ejecución por Consola:\*\*

&#x20;  ```bash

&#x20;  dotnet restore

&#x20;  dotnet build

&#x20;  dotnet run --project SistemaTransportes.UI/SistemaTransportes.UI.csproj

&#x20;  ```



\### Credenciales de Prueba (Accesos Rápidos)

\* \*\*Ventanilla 1:\*\* Operador: `OpControl` | Clave: `1598753`

\* \*\*Ventanilla 2 (Concurrencia):\*\* Operador: `OpVenta2` | Clave: `1598753`

