using System.Text.RegularExpressions;
using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.Domain.Exceptions;
using SistemaTransportes.Domain.Repositories;

namespace SistemaTransportes.Application.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private static readonly Regex DniRegex = new(@"^\d{8}$", RegexOptions.Compiled);
    private static readonly Regex TelefonoRegex = new(@"^\d{6,15}$", RegexOptions.Compiled);

    public VentaService(IVentaRepository ventaRepository)
    {
        _ventaRepository = ventaRepository ?? throw new ArgumentNullException(nameof(ventaRepository));
    }

    public async Task RegistrarVentaAsync(RegistroVentaRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.CajaTurnoId <= 0)
        {
            throw new VentaValidationException("Debe asociarse a un turno de caja activo válido.");
        }

        bool tienePasajes = request.Pasajeros != null && request.Pasajeros.Count > 0;
        bool tieneEncomienda = request.Encomienda != null;

        if (!tienePasajes && !tieneEncomienda)
        {
            throw new VentaValidationException("Debe seleccionar al menos un pasaje o registrar una encomienda para procesar la venta.");
        }

        // Validación de pasajes
        var boletos = new List<Boleto>();
        if (tienePasajes)
        {
            if (request.Pasajeros!.Count > 5)
            {
                throw new VentaValidationException("No se permite comprar más de 5 asientos por transacción.");
            }

            if (request.ViajeId <= 0)
            {
                throw new VentaValidationException("Debe seleccionar un viaje válido para emitir los pasajes.");
            }

            foreach (var p in request.Pasajeros)
            {
                if (string.IsNullOrWhiteSpace(p.Dni) || !DniRegex.IsMatch(p.Dni.Trim()))
                {
                    throw new VentaValidationException($"El DNI '{p.Dni}' del asiento #{p.NroAsiento} es inválido. Debe contener exactamente 8 dígitos.");
                }

                if (string.IsNullOrWhiteSpace(p.Nombres))
                {
                    throw new VentaValidationException($"Debe ingresar el nombre del pasajero para el asiento #{p.NroAsiento}.");
                }

                if (p.Precio < 0)
                {
                    throw new VentaValidationException($"El precio del pasaje para el asiento #{p.NroAsiento} no puede ser negativo.");
                }

                boletos.Add(new Boleto
                {
                    ViajeID = request.ViajeId,
                    CajaTurnoID = request.CajaTurnoId,
                    NroAsiento = p.NroAsiento,
                    DniPasajero = p.Dni.Trim(),
                    NombrePasajero = p.Nombres.Trim(),
                    PrecioFinal = p.Precio,
                    FechaEmision = DateTime.Now,
                    MetodoPago = string.IsNullOrWhiteSpace(request.MetodoPago) ? "Efectivo" : request.MetodoPago,
                    NumeroOperacion = string.IsNullOrWhiteSpace(request.NumeroOperacion) ? null : request.NumeroOperacion.Trim()
                });
            }
        }

        // Validación de encomienda
        Encomienda? encomienda = null;
        if (tieneEncomienda)
        {
            var encDto = request.Encomienda!;

            if (string.IsNullOrWhiteSpace(encDto.Descripcion))
            {
                throw new VentaValidationException("Debe ingresar la descripción detallada del paquete para la encomienda.");
            }

            if (encDto.PesoKg <= 0 || encDto.PesoKg > 50)
            {
                throw new VentaValidationException("El peso de la encomienda debe ser mayor a 0 kg y no puede superar los 50 kg.");
            }

            if (encDto.CostoCarga < 0)
            {
                throw new VentaValidationException("El costo de la carga de la encomienda no puede ser negativo.");
            }

            if (!string.IsNullOrWhiteSpace(encDto.RemitenteTelefono) && !TelefonoRegex.IsMatch(encDto.RemitenteTelefono.Trim()))
            {
                throw new VentaValidationException("El teléfono del remitente debe ser numérico y tener entre 6 y 15 dígitos.");
            }

            if (!string.IsNullOrWhiteSpace(encDto.DestinatarioTelefono) && !TelefonoRegex.IsMatch(encDto.DestinatarioTelefono.Trim()))
            {
                throw new VentaValidationException("El teléfono del destinatario debe ser numérico y tener entre 6 y 15 dígitos.");
            }

            if (encDto.ModalidadEntrega == "Domicilio" && string.IsNullOrWhiteSpace(encDto.DireccionEntrega))
            {
                throw new VentaValidationException("Para la modalidad de entrega a domicilio, debe ingresar una dirección válida.");
            }

            decimal totalCostoEncomienda = encDto.CostoCarga + (encDto.RecargoDelivery ?? 0m);

            encomienda = new Encomienda
            {
                ViajeID = request.ViajeId > 0 ? request.ViajeId : null,
                CajaTurnoID = request.CajaTurnoId,
                Descripcion = encDto.Descripcion.Trim(),
                PesoKg = encDto.PesoKg,
                CostoCarga = totalCostoEncomienda,
                FechaRecepcion = DateTime.Now,
                RemitenteTipoDoc = encDto.RemitenteTipoDoc,
                RemitenteDoc = encDto.RemitenteDoc?.Trim(),
                RemitenteNombre = encDto.RemitenteNombre?.Trim(),
                RemitenteTelefono = encDto.RemitenteTelefono?.Trim(),
                DestinatarioTipoDoc = encDto.DestinatarioTipoDoc,
                DestinatarioDoc = encDto.DestinatarioDoc?.Trim(),
                DestinatarioNombre = encDto.DestinatarioNombre?.Trim(),
                DestinatarioTelefono = encDto.DestinatarioTelefono?.Trim(),
                ModalidadEntrega = string.IsNullOrWhiteSpace(encDto.ModalidadEntrega) ? "Agencia" : encDto.ModalidadEntrega.Trim(),
                DireccionEntrega = encDto.DireccionEntrega?.Trim(),
                RecargoDelivery = encDto.RecargoDelivery,
                MetodoPago = string.IsNullOrWhiteSpace(request.MetodoPago) ? "Efectivo" : request.MetodoPago,
                NumeroOperacion = string.IsNullOrWhiteSpace(request.NumeroOperacion) ? null : request.NumeroOperacion.Trim()
            };
        }

        // Delegar transacción ACID al repositorio de infraestructura
        await _ventaRepository.RegistrarVentaTransaccionalAsync(boletos, encomienda, request.CajaTurnoId, request.MontoTotal);
    }
}
