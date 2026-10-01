using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using SistemaTransportes.Application.DTOs;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Exceptions;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class VentaIntegradaViewModel : ViewModelBase
{
    private readonly IVentaService _ventaService;
    private readonly IConsultaViajesService _consultaViajesService;
    private readonly ICajaTurnoRepository _cajaTurnoRepository;

    public event Action? SolicitarCerrarSesion;
    public event Action<UsuarioSessionModel>? SolicitarAbrirHistorial;
    public event Action<string, string, MessageBoxImage>? NotificarMensaje;

    private UsuarioSessionModel _session = new();
    public UsuarioSessionModel Session
    {
        get => _session;
        set => SetProperty(ref _session, value);
    }

    private decimal _saldoCajaActual = 0.00m;
    public decimal SaldoCajaActual
    {
        get => _saldoCajaActual;
        set => SetProperty(ref _saldoCajaActual, value);
    }

    // --- Búsqueda y Rutas ---
    public ObservableCollection<string> OrigenesDisponibles { get; } = new();
    public ObservableCollection<string> DestinosDisponibles { get; } = new();

    private string? _origenSeleccionado;
    public string? OrigenSeleccionado
    {
        get => _origenSeleccionado;
        set
        {
            if (SetProperty(ref _origenSeleccionado, value))
            {
                _ = CargarDestinosPorOrigenAsync(value);
            }
        }
    }

    private string? _destinoSeleccionado;
    public string? DestinoSeleccionado
    {
        get => _destinoSeleccionado;
        set => SetProperty(ref _destinoSeleccionado, value);
    }

    // --- RANGOS Y PROPIEDADES DE FECHA ---
    public DateTime FechaMinima => DateTime.Today;
    public DateTime FechaMaximaSalida => DateTime.Today.AddDays(60);

    private DateTime? _fechaIda = DateTime.Today;
    public DateTime? FechaIda
    {
        get => _fechaIda ?? DateTime.Today;
        set
        {
            var valor = value ?? DateTime.Today;
            if (valor < FechaMinima) valor = FechaMinima;
            if (valor > FechaMaximaSalida) valor = FechaMaximaSalida;

            if (SetProperty(ref _fechaIda, valor))
            {
                OnPropertyChanged(nameof(FechaVueltaMinima));
                OnPropertyChanged(nameof(FechaVueltaMaxima));

                if (FechaVuelta.HasValue && (FechaVuelta.Value < valor || FechaVuelta.Value > FechaVueltaMaxima))
                {
                    FechaVuelta = null;
                }

                if (MostrarResultadosViajes)
                {
                    _ = BuscarViajesAsync();
                }
            }
        }
    }

    public DateTime FechaVueltaMinima => FechaIda ?? DateTime.Today;
    public DateTime FechaVueltaMaxima => (FechaIda ?? DateTime.Today).AddDays(30);

    private DateTime? _fechaVuelta;
    public DateTime? FechaVuelta
    {
        get => _fechaVuelta;
        set
        {
            if (value.HasValue && (value.Value < FechaVueltaMinima || value.Value > FechaVueltaMaxima))
            {
                return;
            }

            if (SetProperty(ref _fechaVuelta, value))
            {
                if (MostrarResultadosViajes)
                {
                    _ = BuscarViajesAsync();
                }
            }
        }
    }

    // --- Navegación entre Paneles del Centro ---
    private bool _mostrarResultadosViajes;
    public bool MostrarResultadosViajes
    {
        get => _mostrarResultadosViajes;
        set => SetProperty(ref _mostrarResultadosViajes, value);
    }

    private bool _mostrarMapaAsientos;
    public bool MostrarMapaAsientos
    {
        get => _mostrarMapaAsientos;
        set => SetProperty(ref _mostrarMapaAsientos, value);
    }

    private bool _mostrarPanelViajes = true;
    public bool MostrarPanelViajes
    {
        get => _mostrarPanelViajes;
        set => SetProperty(ref _mostrarPanelViajes, value);
    }

    private bool _mostrarPanelPasajeros;
    public bool MostrarPanelPasajeros
    {
        get => _mostrarPanelPasajeros;
        set => SetProperty(ref _mostrarPanelPasajeros, value);
    }

    private bool _mostrarPanelPago;
    public bool MostrarPanelPago
    {
        get => _mostrarPanelPago;
        set => SetProperty(ref _mostrarPanelPago, value);
    }

    public ObservableCollection<PasajeroItemViewModel> Pasajeros { get; } = new();

    // --- Asientos y Croquis ---
    private int _pisoActual = 1;
    public ObservableCollection<FilaAsientoItemViewModel> FilasAsientosVisibles { get; } = new();
    public ObservableCollection<AsientoItemViewModel> AsientosSeleccionados { get; } = new();
    private readonly List<AsientoItemViewModel> _todosLosAsientos = new();

    private bool _cargandoAsientos;
    public bool CargandoAsientos
    {
        get => _cargandoAsientos;
        set => SetProperty(ref _cargandoAsientos, value);
    }

    // --- Viajes Disponibles ---
    public ObservableCollection<ViajeItemViewModel> ViajesDisponibles { get; } = new();

    private ViajeItemViewModel? _viajeSeleccionado;
    public ViajeItemViewModel? ViajeSeleccionado
    {
        get => _viajeSeleccionado;
        set
        {
            if (SetProperty(ref _viajeSeleccionado, value))
            {
                OnPropertyChanged(nameof(TextoBotonContinuar));
                OnPropertyChanged(nameof(TerminalLlegadaDisplay));
                CalcularLiquidacion();
            }
        }
    }

    // --- Modalidad y Carga ---
    private bool _esSoloEncomienda;
    public bool EsSoloEncomienda
    {
        get => _esSoloEncomienda;
        set
        {
            if (!SetProperty(ref _esSoloEncomienda, value))
            {
                return;
            }

            OnPropertyChanged(nameof(EsModoPasajeConEquipaje));
            OnPropertyChanged(nameof(TarifaPorKg));
            OnPropertyChanged(nameof(RecargoDelivery));
            OnPropertyChanged(nameof(TerminalLlegadaDisplay));
            OnPropertyChanged(nameof(TextoBotonContinuarAPago));
            OnPropertyChanged(nameof(TextoBotonVolverDePago));
            OnPropertyChanged(nameof(TextoBotonVolverDePaso2));

            foreach (var p in PresetsDisponibles)
            {
                p.ActualizarModo(_esSoloEncomienda);
            }

            if (_esSoloEncomienda)
            {
                if (!IncluyeEncomienda)
                {
                    IncluyeEncomienda = true;
                }
                else if (PresetSeleccionado == null)
                {
                    PresetSeleccionado = PresetsDisponibles.FirstOrDefault(p => p.CoincideCon("Caja Chica"))
                                        ?? PresetsDisponibles.FirstOrDefault();
                }
                else
                {
                    EncomiendaCosto = PresetSeleccionado.EsPersonalizado
                        ? Math.Round(EncomiendaPesoKg * TarifaPorKg, 2)
                        : PresetSeleccionado.Tarifa;
                }

                LimpiarSeleccionAsientos();
            }
            else if (PresetSeleccionado != null)
            {
                EncomiendaCosto = PresetSeleccionado.EsPersonalizado
                    ? Math.Round(EncomiendaPesoKg * TarifaPorKg, 2)
                    : PresetSeleccionado.Tarifa;
            }

            CalcularLiquidacion();
        }
    }

    public bool EsModoPasajeConEquipaje
    {
        get => !_esSoloEncomienda;
        set => EsSoloEncomienda = !value;
    }

    private bool _incluyeEncomienda;
    public bool IncluyeEncomienda
    {
        get => _incluyeEncomienda;
        set
        {
            if (!SetProperty(ref _incluyeEncomienda, value))
            {
                return;
            }

            if (!_incluyeEncomienda)
            {
                PresetSeleccionado = null;
                EsCargaPersonalizada = false;
                EncomiendaDescripcion = string.Empty;
                _encomiendaPesoKg = 0m;
                _encomiendaPesoKgTexto = "0";
                OnPropertyChanged(nameof(EncomiendaPesoKg));
                OnPropertyChanged(nameof(EncomiendaPesoKgTexto));
                OnPropertyChanged(nameof(EsPesoMaximoEncomienda));
                _encomiendaCosto = 0m;
                OnPropertyChanged(nameof(EncomiendaCosto));
            }
            else
            {
                PresetSeleccionado = PresetsDisponibles.FirstOrDefault(p => p.CoincideCon("Caja Chica"))
                                    ?? PresetsDisponibles.FirstOrDefault();
            }

            CalcularLiquidacion();
        }
    }

    public ObservableCollection<PresetItemViewModel> PresetsDisponibles { get; } = new()
    {
        new PresetItemViewModel
        {
            Titulo = "Sobre / Documento",
            Nombre = "Sobre / Documento",
            Descripcion = "Documentos, sobres, cartas y correspondencia legal.",
            TarifaPasajero = 10.00m,
            TarifaSoloEncomienda = 12.00m,
            Tarifa = 10.00m,
            PesoRef = 0.5m,
            EsPersonalizado = false,
            Icono = "✉️"
        },
        new PresetItemViewModel
        {
            Titulo = "Caja Chica",
            Nombre = "Caja Chica (Calzado / Paquete pequeño)",
            Descripcion = "Calzado, accesorios personales, repuestos o encomienda compacta.",
            TarifaPasajero = 15.00m,
            TarifaSoloEncomienda = 20.00m,
            Tarifa = 15.00m,
            PesoRef = 3.0m,
            EsPersonalizado = false,
            Icono = "📦"
        },
        new PresetItemViewModel
        {
            Titulo = "Caja Mediana",
            Nombre = "Caja Mediana (Abarrotes / Menaje)",
            Descripcion = "Víveres, abarrotes, menaje o paquetes medianos.",
            TarifaPasajero = 25.00m,
            TarifaSoloEncomienda = 30.00m,
            Tarifa = 25.00m,
            PesoRef = 8.0m,
            EsPersonalizado = false,
            Icono = "📦"
        },
        new PresetItemViewModel
        {
            Titulo = "Caja Grande",
            Nombre = "Caja Grande (Electrodoméstico / Caja pesada)",
            Descripcion = "Electrodomésticos, equipos de sonido, cajas de volumen amplio.",
            TarifaPasajero = 40.00m,
            TarifaSoloEncomienda = 50.00m,
            Tarifa = 40.00m,
            PesoRef = 15.0m,
            EsPersonalizado = false,
            Icono = "📦"
        },
        new PresetItemViewModel
        {
            Titulo = "Saco / Costal / Fardo",
            Nombre = "Saco / Costal / Fardo (Bodega andina / selva)",
            Descripcion = "Costales agrícolas, sacos de granos, fardos de ropa o productos regionales.",
            TarifaPasajero = 35.00m,
            TarifaSoloEncomienda = 45.00m,
            Tarifa = 35.00m,
            PesoRef = 20.0m,
            EsPersonalizado = false,
            Icono = "🌾"
        },
        new PresetItemViewModel
        {
            Titulo = "Carga Especial Voluminosa",
            Nombre = "Carga Especial Voluminosa (Bicicleta / TV / Instrumento / Cochecito)",
            Descripcion = "Bicicletas, televisores pantalla plana, guitarras, coches para bebé.",
            TarifaPasajero = 50.00m,
            TarifaSoloEncomienda = 65.00m,
            Tarifa = 50.00m,
            PesoRef = 15.0m,
            EsPersonalizado = false,
            Icono = "🚲"
        },
        new PresetItemViewModel
        {
            Titulo = "Personalizado",
            Nombre = "Personalizado (Medición y peso manual)",
            Descripcion = "Tarifa según peso exacto en balanza (máx. 50 kg).",
            TarifaPasajero = 0.00m,
            TarifaSoloEncomienda = 0.00m,
            Tarifa = 0.00m,
            PesoRef = 0.0m,
            EsPersonalizado = true,
            Icono = "⚖️"
        }
    };

    private PresetItemViewModel? _presetSeleccionado;
    public PresetItemViewModel? PresetSeleccionado
    {
        get => _presetSeleccionado;
        set
        {
            if (!SetProperty(ref _presetSeleccionado, value))
            {
                return;
            }

            if (_presetSeleccionado == null)
            {
                EsCargaPersonalizada = false;
                return;
            }

            if (_presetSeleccionado.EsPersonalizado)
            {
                EsCargaPersonalizada = true;

                if (string.IsNullOrWhiteSpace(EncomiendaDescripcion)
                    || PresetsDisponibles.Any(p => !p.EsPersonalizado && p.Nombre == EncomiendaDescripcion))
                {
                    EncomiendaDescripcion = "Carga especial / Equipaje adicional";
                }

                EncomiendaCosto = EncomiendaPesoKg <= 0m
                    ? 0m
                    : Math.Round(EncomiendaPesoKg * TarifaPorKg, 2);

                return;
            }

            EsCargaPersonalizada = false;
            EncomiendaDescripcion = _presetSeleccionado.Nombre;

            _encomiendaPesoKg = _presetSeleccionado.PesoRef;
            _encomiendaPesoKgTexto = _presetSeleccionado.PesoRef.ToString("0.##", CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(EncomiendaPesoKg));
            OnPropertyChanged(nameof(EncomiendaPesoKgTexto));
            OnPropertyChanged(nameof(EsPesoMaximoEncomienda));

            _encomiendaCosto = _presetSeleccionado.Tarifa;
            OnPropertyChanged(nameof(EncomiendaCosto));
            CalcularLiquidacion();
        }
    }

    private string _modalidadEntrega = "Agencia";
    public string ModalidadEntrega
    {
        get => _modalidadEntrega;
        set
        {
            if (SetProperty(ref _modalidadEntrega, value))
            {
                OnPropertyChanged(nameof(EsEntregaAgencia));
                OnPropertyChanged(nameof(EsEntregaDomicilio));
                OnPropertyChanged(nameof(RecargoDelivery));
                CalcularLiquidacion();
            }
        }
    }

    public bool EsEntregaAgencia
    {
        get => string.Equals(ModalidadEntrega, "Agencia", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value) ModalidadEntrega = "Agencia";
        }
    }

    public bool EsEntregaDomicilio
    {
        get => string.Equals(ModalidadEntrega, "Domicilio", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value) ModalidadEntrega = "Domicilio";
        }
    }

    public decimal RecargoDelivery => (EsSoloEncomienda && EsEntregaDomicilio) ? 10.00m : 0.00m;
    public const decimal LimiteMaximoPesoKg = 50.0m;

    private decimal _encomiendaCosto = 15.00m;
    public decimal EncomiendaCosto
    {
        get => _encomiendaCosto;
        set
        {
            if (SetProperty(ref _encomiendaCosto, value))
            {
                CalcularLiquidacion();
            }
        }
    }

    public decimal TarifaPorKg => EsSoloEncomienda ? 4.00m : 3.00m;

    private string _encomiendaPesoKgTexto = "3.0";
    public string EncomiendaPesoKgTexto
    {
        get => _encomiendaPesoKgTexto;
        set
        {
            if (_encomiendaPesoKgTexto == value) return;

            _encomiendaPesoKgTexto = value ?? string.Empty;
            OnPropertyChanged();

            string limpio = _encomiendaPesoKgTexto.Trim().Replace(',', '.');

            if (string.IsNullOrWhiteSpace(limpio) || limpio == "." || limpio == "-")
            {
                _encomiendaPesoKg = 0m;
                OnPropertyChanged(nameof(EncomiendaPesoKg));
                OnPropertyChanged(nameof(EsPesoMaximoEncomienda));

                if (EsCargaPersonalizada)
                {
                    EncomiendaCosto = 0m;
                }
            }
            else if (decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed))
            {
                parsed = Math.Clamp(parsed, 0m, LimiteMaximoPesoKg);

                _encomiendaPesoKg = parsed;
                OnPropertyChanged(nameof(EncomiendaPesoKg));
                OnPropertyChanged(nameof(EsPesoMaximoEncomienda));

                if (EsCargaPersonalizada)
                {
                    EncomiendaCosto = Math.Round(parsed * TarifaPorKg, 2);
                }
            }
        }
    }

    private decimal _encomiendaPesoKg = 3.0m;
    public decimal EncomiendaPesoKg
    {
        get => _encomiendaPesoKg;
        set
        {
            decimal acotado = Math.Clamp(value, 0m, LimiteMaximoPesoKg);
            bool cambioValor = _encomiendaPesoKg != acotado;
            _encomiendaPesoKg = acotado;

            string strVal = acotado.ToString("0.##", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(_encomiendaPesoKgTexto) && _encomiendaPesoKgTexto != strVal)
            {
                _encomiendaPesoKgTexto = strVal;
                OnPropertyChanged(nameof(EncomiendaPesoKgTexto));
            }
            else if (string.IsNullOrWhiteSpace(_encomiendaPesoKgTexto) && acotado > 0m)
            {
                _encomiendaPesoKgTexto = strVal;
                OnPropertyChanged(nameof(EncomiendaPesoKgTexto));
            }

            if (!cambioValor) return;

            OnPropertyChanged();
            OnPropertyChanged(nameof(EsPesoMaximoEncomienda));

            if (EsCargaPersonalizada)
            {
                EncomiendaCosto = Math.Round(acotado * TarifaPorKg, 2);
            }
        }
    }

    private string _encomiendaDescripcion = "Caja Chica (Calzado / Paquete pequeño)";
    public string EncomiendaDescripcion
    {
        get => _encomiendaDescripcion;
        set => SetProperty(ref _encomiendaDescripcion, value);
    }

    private bool _esCargaPersonalizada;
    public bool EsCargaPersonalizada
    {
        get => _esCargaPersonalizada;
        set => SetProperty(ref _esCargaPersonalizada, value);
    }

    public bool EsPesoMaximoEncomienda => EncomiendaPesoKg >= LimiteMaximoPesoKg;
    public string TerminalLlegadaDisplay => ViajeSeleccionado != null
        ? $"Agencia Central {ViajeSeleccionado.Destino} — Terminal Terrestre"
        : "Agencia Central de Destino — Terminal Terrestre";

    private string _direccionEntrega = "";
    public string DireccionEntrega
    {
        get => _direccionEntrega;
        set => SetProperty(ref _direccionEntrega, value);
    }

    private string _referenciaEntrega = "";
    public string ReferenciaEntrega
    {
        get => _referenciaEntrega;
        set => SetProperty(ref _referenciaEntrega, value);
    }

    // Encomienda Remitente / Destinatario
    public ObservableCollection<string> TiposDocumentoDisponibles { get; } = new() { "DNI", "RUC", "CE", "Pasaporte" };

    private string _remitenteTipoDoc = "DNI";
    public string RemitenteTipoDoc
    {
        get => _remitenteTipoDoc;
        set
        {
            if (SetProperty(ref _remitenteTipoDoc, value))
            {
                OnPropertyChanged(nameof(RemitenteDocMaxLength));
            }
        }
    }

    private string _remitenteDoc = "";
    public string RemitenteDoc
    {
        get => _remitenteDoc;
        set => SetProperty(ref _remitenteDoc, value);
    }

    public int RemitenteDocMaxLength => RemitenteTipoDoc switch
    {
        "RUC" => 11,
        "Pasaporte" => 12,
        "CE" or "C.E." => 12,
        _ => 8
    };

    private string _remitenteNombre = "";
    public string RemitenteNombre
    {
        get => _remitenteNombre;
        set => SetProperty(ref _remitenteNombre, value);
    }

    private string _remitenteTelefono = "";
    public string RemitenteTelefono
    {
        get => _remitenteTelefono;
        set => SetProperty(ref _remitenteTelefono, value);
    }

    private string _destinatarioTipoDoc = "DNI";
    public string DestinatarioTipoDoc
    {
        get => _destinatarioTipoDoc;
        set
        {
            if (SetProperty(ref _destinatarioTipoDoc, value))
            {
                OnPropertyChanged(nameof(DestinatarioDocMaxLength));
            }
        }
    }

    private string _destinatarioDoc = "";
    public string DestinatarioDoc
    {
        get => _destinatarioDoc;
        set => SetProperty(ref _destinatarioDoc, value);
    }

    public int DestinatarioDocMaxLength => DestinatarioTipoDoc switch
    {
        "RUC" => 11,
        "Pasaporte" => 12,
        "CE" or "C.E." => 12,
        _ => 8
    };

    private string _destinatarioNombre = "";
    public string DestinatarioNombre
    {
        get => _destinatarioNombre;
        set => SetProperty(ref _destinatarioNombre, value);
    }

    private string _destinatarioTelefono = "";
    public string DestinatarioTelefono
    {
        get => _destinatarioTelefono;
        set => SetProperty(ref _destinatarioTelefono, value);
    }

    // --- Liquidación de Venta ---
    public decimal TotalBoletos => EsSoloEncomienda ? 0.00m : AsientosSeleccionados.Count * (ViajeSeleccionado?.PrecioBase ?? 0m);
    public decimal TotalVenta => TotalBoletos + (EsSoloEncomienda || IncluyeEncomienda ? EncomiendaCosto + RecargoDelivery : 0m);

    public bool PuedeContinuarAPasajeros => EsSoloEncomienda
        ? (ViajeSeleccionado != null && IncluyeEncomienda && TotalVenta > 0m)
        : AsientosSeleccionados.Count >= 1;

    public string TextoBotonContinuar
    {
        get
        {
            if (EsSoloEncomienda)
            {
                return ViajeSeleccionado == null
                    ? "Seleccione una salida de viaje para la encomienda"
                    : $"Continuar a Guía de Despacho (S/. {TotalVenta:N2}) ➔";
            }

            return AsientosSeleccionados.Count == 0
                ? "Seleccione al menos 1 asiento"
                : $"Continuar con Datos de Pasajeros ({AsientosSeleccionados.Count} Asiento{(AsientosSeleccionados.Count > 1 ? "s" : "")}) ➔";
        }
    }

    public string TextoBotonVolverDePaso2 => "← Volver a Selección de Viaje";
    public string TextoBotonContinuarAPago => EsSoloEncomienda
        ? $"Continuar a Liquidación y Pago (S/. {TotalVenta:N2}) ➔"
        : "Continuar al Pago ➔";
    public string TextoBotonVolverDePago =>
        EsSoloEncomienda ? "Volver a Guía de Despacho" : "Volver a Datos de Pasajeros";
    public string TextoBotonConfirmarPago => EsSoloEncomienda
        ? $"Confirmar Pago y Despachar Encomienda (S/. {TotalVenta:N2})"
        : $"Confirmar Pago y Emitir Boletos (S/. {TotalVenta:N2})";
    public string TextoBotonFooterConfirmar =>
        EsSoloEncomienda ? "✓ CONFIRMAR Y DESPACHAR" : "✓ CONFIRMAR PAGO Y EMITIR";

    // --- Pago ---
    private string _metodoPagoSeleccionado = "Efectivo";
    public string MetodoPagoSeleccionado
    {
        get => _metodoPagoSeleccionado;
        set
        {
            if (SetProperty(ref _metodoPagoSeleccionado, value))
            {
                OnPropertyChanged(nameof(EsPagoEfectivo));
                OnPropertyChanged(nameof(EsPagoDigital));
                OnPropertyChanged(nameof(MaxLongitudOperacion));

                if (!string.IsNullOrEmpty(_nroOperacion) && _nroOperacion.Length > MaxLongitudOperacion)
                {
                    NroOperacion = _nroOperacion[..MaxLongitudOperacion];
                }

                CalcularLiquidacion();
            }
        }
    }

    public bool EsPagoEfectivo => MetodoPagoSeleccionado == "Efectivo";
    public bool EsPagoDigital => MetodoPagoSeleccionado != "Efectivo";

    public int MaxLongitudOperacion => MetodoPagoSeleccionado switch
    {
        "Yape / Plin" => 8,
        "Tarjeta" => 17,
        _ => 20
    };

    private decimal? _montoRecibido = 100.00m;
    public decimal? MontoRecibido
    {
        get => _montoRecibido;
        set
        {
            if (SetProperty(ref _montoRecibido, value))
            {
                OnPropertyChanged(nameof(MontoRecibidoSeguro));
                OnPropertyChanged(nameof(Vuelto));
                OnPropertyChanged(nameof(FaltaDinero));
                OnPropertyChanged(nameof(DiferenciaFaltante));
            }
        }
    }

    public decimal MontoRecibidoSeguro => MontoRecibido ?? 0m;
    public decimal Vuelto => MontoRecibidoSeguro >= TotalVenta ? MontoRecibidoSeguro - TotalVenta : 0m;
    public bool FaltaDinero => EsPagoEfectivo && (MontoRecibido == null || MontoRecibido < TotalVenta);
    public decimal DiferenciaFaltante => TotalVenta - MontoRecibidoSeguro;

    private string _nroOperacion = "";
    public string NroOperacion
    {
        get => _nroOperacion;
        set
        {
            string soloNumeros = value != null
                ? new string(value.Where(char.IsDigit).ToArray())
                : string.Empty;

            if (soloNumeros.Length > MaxLongitudOperacion)
            {
                soloNumeros = soloNumeros[..MaxLongitudOperacion];
            }

            SetProperty(ref _nroOperacion, soloNumeros);
        }
    }

    // Alertas
    public bool MostrarAlerta { get; set; }
    public string MensajeAlerta { get; set; } = "";
    public string TipoAlerta { get; set; } = "Info";

    // --- Comandos ---
    public ICommand BuscarViajesCommand { get; }
    public ICommand LimpiarBusquedaCommand { get; }
    public ICommand IntercambiarCiudadesCommand { get; }
    public ICommand OrdenarPorSalidaCommand { get; }
    public ICommand OrdenarPorPrecioCommand { get; }
    public ICommand SeleccionarViajeCommand { get; }
    public ICommand CambiarPisoCommand { get; }
    public ICommand IrAPasajerosCommand { get; }
    public ICommand VolverAViajesCommand { get; }
    public ICommand IrAPagoCommand { get; }
    public ICommand VolverAPasajerosCommand { get; }
    public ICommand ConfirmarVentaFinalCommand { get; }
    public ICommand ConfirmarVentaCommand => ConfirmarVentaFinalCommand;
    public ICommand CerrarSesionCommand { get; }
    public ICommand RefrescarMapaCommand { get; }
    public ICommand MontoRapidoCommand { get; }
    public ICommand AbrirHistorialVentasCommand { get; }

    public VentaIntegradaViewModel(
        IVentaService ventaService,
        IConsultaViajesService consultaViajesService,
        ICajaTurnoRepository cajaTurnoRepository)
    {
        _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
        _consultaViajesService = consultaViajesService ?? throw new ArgumentNullException(nameof(consultaViajesService));
        _cajaTurnoRepository = cajaTurnoRepository ?? throw new ArgumentNullException(nameof(cajaTurnoRepository));

        _incluyeEncomienda = false;
        _presetSeleccionado = null;
        _encomiendaCosto = 0m;
        _encomiendaPesoKg = 0m;
        _encomiendaPesoKgTexto = "0";
        _encomiendaDescripcion = string.Empty;
        _esCargaPersonalizada = false;

        BuscarViajesCommand = new RelayCommand(async () => await BuscarViajesAsync());
        LimpiarBusquedaCommand = new RelayCommand(LimpiarBusqueda);
        IntercambiarCiudadesCommand = new RelayCommand(IntercambiarCiudades);
        OrdenarPorSalidaCommand = new RelayCommand(() => OrdenarViajes(v => v.FechaHoraSalida));
        OrdenarPorPrecioCommand = new RelayCommand(() => OrdenarViajes(v => v.PrecioBase));
        SeleccionarViajeCommand = new RelayCommand<ViajeItemViewModel>(async v => await SeleccionarViajeAsync(v));
        CambiarPisoCommand = new RelayCommand<object>(p => CambiarPiso(Convert.ToInt32(p)));
        IrAPasajerosCommand = new RelayCommand(IrAPasajeros, () => PuedeContinuarAPasajeros);
        VolverAViajesCommand = new RelayCommand(VolverAViajes);
        IrAPagoCommand = new RelayCommand(IrAPago);
        VolverAPasajerosCommand = new RelayCommand(VolverAPasajeros);
        ConfirmarVentaFinalCommand = new RelayCommand(async () => await ConfirmarVentaAsync());
        CerrarSesionCommand = new RelayCommand(() => SolicitarCerrarSesion?.Invoke());
        RefrescarMapaCommand = new RelayCommand(async () => await RefrescarMapaAsync());
        MontoRapidoCommand = new RelayCommand<string>(AplicarMontoRapido);
        AbrirHistorialVentasCommand = new RelayCommand(() => SolicitarAbrirHistorial?.Invoke(Session));
    }

    public void InicializarSesion(UsuarioSessionModel session)
    {
        Session = session ?? new UsuarioSessionModel();
        _ = InicializarDatosDesdeBdAsync();
    }

    public async Task InicializarDatosDesdeBdAsync()
    {
        await CargarDatosInicialesAsync();
    }

    public async Task InicializarSesionAsync(UsuarioSessionModel session)
    {
        Session = session ?? new UsuarioSessionModel();
        await InicializarDatosDesdeBdAsync();
    }

    public async Task CargarDatosInicialesAsync()
    {
        try
        {
            if (Session.CajaTurnoID > 0)
            {
                SaldoCajaActual = await _cajaTurnoRepository.ObtenerSaldoActualAsync(Session.CajaTurnoID);
            }

            var origenes = await _consultaViajesService.ObtenerOrigenesAsync();
            OrigenesDisponibles.Clear();
            foreach (var orig in origenes)
            {
                OrigenesDisponibles.Add(orig);
            }

            OrigenSeleccionado = null;
            DestinoSeleccionado = null;
            MostrarResultadosViajes = false;
        }
        catch (Exception ex)
        {
            NotificarMensaje?.Invoke($"Error al cargar datos iniciales:\n{ex.Message}", "Error de Conexión", MessageBoxImage.Warning);
        }
    }

    private async Task CargarDestinosPorOrigenAsync(string? origen)
    {
        DestinosDisponibles.Clear();
        if (string.IsNullOrWhiteSpace(origen))
        {
            DestinoSeleccionado = null;
            return;
        }

        try
        {
            var destinos = await _consultaViajesService.ObtenerDestinosPorOrigenAsync(origen);
            foreach (var d in destinos)
            {
                DestinosDisponibles.Add(d);
            }

            DestinoSeleccionado = DestinosDisponibles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar destinos para {origen}: {ex.Message}");
        }
    }

    public async Task BuscarViajesAsync()
    {
        ViajesDisponibles.Clear();
        ViajeSeleccionado = null;
        MostrarMapaAsientos = false;
        AsientosSeleccionados.Clear();
        _todosLosAsientos.Clear();
        FilasAsientosVisibles.Clear();

        string? origen = string.IsNullOrWhiteSpace(OrigenSeleccionado) ? null : OrigenSeleccionado;
        string? destino = string.IsNullOrWhiteSpace(DestinoSeleccionado) ? null : DestinoSeleccionado;
        DateTime fechaBase = FechaIda ?? DateTime.Today;

        try
        {
            var viajes = await _consultaViajesService.BuscarViajesAsync(origen, destino, fechaBase);
            bool esIdaYVuelta = FechaVuelta.HasValue;
            decimal factorTarifa = esIdaYVuelta ? 1.75m : 1.00m;

            foreach (var v in viajes)
            {
                decimal precioCalculado = Math.Round(v.PrecioBase * factorTarifa, 2);

                DateTime fechaSalidaAjustada = new DateTime(
                    fechaBase.Year, fechaBase.Month, fechaBase.Day,
                    v.FechaSalida.Hour, v.FechaSalida.Minute, v.FechaSalida.Second);

                TimeSpan duracionSpan = v.FechaHoraLlegada.HasValue
                    ? (v.FechaHoraLlegada.Value - v.FechaSalida)
                    : TimeSpan.FromHours(7.5);

                DateTime fechaLlegadaAjustada = fechaSalidaAjustada.Add(duracionSpan);

                string horaSalidaTexto = fechaSalidaAjustada.ToString("hh:mm tt", CultureInfo.InvariantCulture).ToUpper();
                string horaLlegadaTexto = fechaLlegadaAjustada.ToString("hh:mm tt", CultureInfo.InvariantCulture).ToUpper();

                string duracion = string.IsNullOrWhiteSpace(v.DuracionEstimada)
                    ? $"{(int)duracionSpan.TotalHours:00}h {duracionSpan.Minutes:00}m"
                    : v.DuracionEstimada;

                ViajesDisponibles.Add(new ViajeItemViewModel
                {
                    ViajeID = v.ViajeID,
                    Origen = v.Origen,
                    Destino = v.Destino,
                    BusPlaca = v.PlacaBus,
                    TipoServicio = v.TipoServicio,
                    Categoria = v.Categoria,
                    HoraSalidaTexto = horaSalidaTexto,
                    HoraLlegadaTexto = horaLlegadaTexto,
                    Duracion = duracion,
                    FechaHoraSalida = fechaSalidaAjustada,
                    FechaHoraLlegada = fechaLlegadaAjustada,
                    PrecioBase = precioCalculado,
                    EsIdaYVuelta = esIdaYVuelta
                });
            }

            MostrarResultadosViajes = true;
        }
        catch (Exception ex)
        {
            NotificarMensaje?.Invoke($"Error al buscar viajes:\n{ex.Message}", "Error de Búsqueda", MessageBoxImage.Error);
        }
    }

    private void LimpiarBusqueda()
    {
        MostrarResultadosViajes = false;
        MostrarMapaAsientos = false;
        OrigenSeleccionado = null;
        DestinoSeleccionado = null;
        FechaIda = DateTime.Today;
        FechaVuelta = null;
        ViajesDisponibles.Clear();
        ViajeSeleccionado = null;
        AsientosSeleccionados.Clear();
        _todosLosAsientos.Clear();
        FilasAsientosVisibles.Clear();
        Pasajeros.Clear();
        MostrarPanelViajes = true;
        MostrarPanelPasajeros = false;
        MostrarPanelPago = false;
        MetodoPagoSeleccionado = "Efectivo";
        MontoRecibido = null;
        NroOperacion = "";
        RemitenteDoc = "";
        RemitenteNombre = "";
        RemitenteTelefono = "";
        DestinatarioDoc = "";
        DestinatarioNombre = "";
        DestinatarioTelefono = "";
        DireccionEntrega = "";
        ReferenciaEntrega = "";
        ModalidadEntrega = "Agencia";
        EsSoloEncomienda = false;
        IncluyeEncomienda = false;
        PresetSeleccionado = PresetsDisponibles.FirstOrDefault();
        CalcularLiquidacion();
    }

    private void IntercambiarCiudades()
    {
        string? tempOrig = OrigenSeleccionado;
        string? tempDest = DestinoSeleccionado;

        if (!string.IsNullOrWhiteSpace(tempDest) && OrigenesDisponibles.Contains(tempDest))
        {
            OrigenSeleccionado = tempDest;
            if (!string.IsNullOrWhiteSpace(tempOrig) && DestinosDisponibles.Contains(tempOrig))
            {
                DestinoSeleccionado = tempOrig;
            }
        }
    }

    private void OrdenarViajes<TKey>(Func<ViajeItemViewModel, TKey> keySelector)
    {
        var ordenados = ViajesDisponibles.OrderBy(keySelector).ToList();
        ViajesDisponibles.Clear();
        foreach (var v in ordenados) ViajesDisponibles.Add(v);
    }

    public async Task SeleccionarViajeAsync(ViajeItemViewModel? viaje)
    {
        if (viaje == null) return;
        if (viaje.SalidaVencida)
        {
            NotificarMensaje?.Invoke(
                $"El bus programado para las {viaje.HoraSalidaTexto} ya partió o su horario de venta está cerrado.",
                "Salida no disponible",
                MessageBoxImage.Information);
            return;
        }

        ViajeSeleccionado = viaje;
        MostrarMapaAsientos = true;
        AsientosSeleccionados.Clear();

        if (!EsSoloEncomienda)
        {
            await ConsultarAsientosAsync(viaje.ViajeID);
        }
        else
        {
            CalcularLiquidacion();
        }
    }

    private async Task ConsultarAsientosAsync(int viajeId)
    {
        CargandoAsientos = true;
        _todosLosAsientos.Clear();

        try
        {
            var estados = await _consultaViajesService.ObtenerMapaAsientosAsync(viajeId);

            foreach (var est in estados)
            {
                // Piso 1: asientos 1..20, Piso 2: asientos 21..40
                int piso = est.NroAsiento <= 20 ? 1 : 2;

                var asientoItem = new AsientoItemViewModel
                {
                    NroAsiento = est.NroAsiento,
                    Piso = piso,
                    Estado = est.Estado
                };
                asientoItem.ClickCommand = new RelayCommand(() => ToggleAsiento(asientoItem));
                _todosLosAsientos.Add(asientoItem);
            }

            _pisoActual = 1;
            CargarAsientos();
        }
        catch (Exception ex)
        {
            NotificarMensaje?.Invoke($"Error al cargar croquis de asientos del bus:\n{ex.Message}", "Error de Asientos", MessageBoxImage.Error);
        }
        finally
        {
            CargandoAsientos = false;
        }
    }

    private void CargarAsientos()
    {
        FilasAsientosVisibles.Clear();
        var pisoAsientos = _todosLosAsientos.Where(a => a.Piso == _pisoActual).OrderBy(a => a.NroAsiento).ToList();

        for (int i = 0; i < pisoAsientos.Count; i += 4)
        {
            var fila = new FilaAsientoItemViewModel
            {
                AsientoVentanaIzq = i < pisoAsientos.Count ? pisoAsientos[i] : null,
                AsientoPasilloIzq = i + 1 < pisoAsientos.Count ? pisoAsientos[i + 1] : null,
                AsientoPasilloDer = i + 2 < pisoAsientos.Count ? pisoAsientos[i + 2] : null,
                AsientoVentanaDer = i + 3 < pisoAsientos.Count ? pisoAsientos[i + 3] : null
            };
            FilasAsientosVisibles.Add(fila);
        }
    }

    private void CambiarPiso(int piso)
    {
        _pisoActual = piso;
        CargarAsientos();
    }

    public void LimpiarSeleccionAsientos()
    {
        foreach (var a in AsientosSeleccionados)
        {
            a.Estado = "Libre";
        }

        AsientosSeleccionados.Clear();
        Pasajeros.Clear();
        CalcularLiquidacion();
    }

    private void ToggleAsiento(AsientoItemViewModel asiento)
    {
        if (EsSoloEncomienda)
        {
            NotificarMensaje?.Invoke(
                "El modo 'Solo Envío de Encomienda' está activo. La carga viaja en bodega y no requiere selección de asientos.",
                "Aviso", MessageBoxImage.Information);
            return;
        }

        if (asiento.Estado == "Ocupado") return;

        if (asiento.Estado == "Seleccionado")
        {
            asiento.Estado = "Libre";
            AsientosSeleccionados.Remove(asiento);
        }
        else
        {
            if (AsientosSeleccionados.Count >= 5)
            {
                NotificarMensaje?.Invoke("Puede seleccionar un máximo de 5 asientos por operación.", "Límite Alcanzado", MessageBoxImage.Information);
                return;
            }
            asiento.Estado = "Seleccionado";
            AsientosSeleccionados.Add(asiento);
        }

        CalcularLiquidacion();
    }

    public async Task RefrescarMapaAsync()
    {
        if (ViajeSeleccionado != null)
        {
            await ConsultarAsientosAsync(ViajeSeleccionado.ViajeID);
        }
        else
        {
            CargarAsientos();
        }
    }

    private void IrAPasajeros()
    {
        if (EsSoloEncomienda)
        {
            if (ViajeSeleccionado == null)
            {
                NotificarMensaje?.Invoke("Seleccione un viaje de destino para la encomienda.", "Validación", MessageBoxImage.Warning);
                return;
            }
            MostrarPanelViajes = false;
            MostrarPanelPasajeros = true;
            MostrarPanelPago = false;
            return;
        }

        Pasajeros.Clear();
        foreach (var a in AsientosSeleccionados.OrderBy(x => x.NroAsiento))
        {
            Pasajeros.Add(new PasajeroItemViewModel
            {
                NroAsiento = a.NroAsiento,
                Piso = a.Piso,
                Precio = ViajeSeleccionado?.PrecioBase ?? 0m
            });
        }
        MostrarPanelViajes = false;
        MostrarPanelPasajeros = true;
        MostrarPanelPago = false;
    }

    private void VolverAViajes()
    {
        MostrarPanelViajes = true;
        MostrarPanelPasajeros = false;
        MostrarPanelPago = false;
    }

    private void IrAPago()
    {
        if (!EsSoloEncomienda)
        {
            if (AsientosSeleccionados.Count == 0)
            {
                NotificarMensaje?.Invoke("Debe seleccionar al menos 1 asiento para continuar.", "Validación", MessageBoxImage.Warning);
                return;
            }

            foreach (var pas in Pasajeros)
            {
                if (string.IsNullOrWhiteSpace(pas.Dni) || pas.Dni.Trim().Length < 8)
                {
                    NotificarMensaje?.Invoke($"Ingrese un DNI válido de 8 dígitos para el Asiento N° {pas.NroAsiento}.", "Datos Incompletos", MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(pas.Nombres) || pas.Nombres.Trim().Length < 3)
                {
                    NotificarMensaje?.Invoke($"Ingrese los nombres completos para el Asiento N° {pas.NroAsiento}.", "Datos Incompletos", MessageBoxImage.Warning);
                    return;
                }
            }
        }
        else
        {
            if (ViajeSeleccionado == null)
            {
                NotificarMensaje?.Invoke("Seleccione un viaje para la encomienda.", "Validación", MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RemitenteDoc) || string.IsNullOrWhiteSpace(RemitenteNombre))
            {
                NotificarMensaje?.Invoke("Ingrese los datos del remitente.", "Datos Incompletos", MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DestinatarioDoc) || string.IsNullOrWhiteSpace(DestinatarioNombre))
            {
                NotificarMensaje?.Invoke("Ingrese los datos del destinatario.", "Datos Incompletos", MessageBoxImage.Warning);
                return;
            }

            if (EsEntregaDomicilio && string.IsNullOrWhiteSpace(DireccionEntrega))
            {
                NotificarMensaje?.Invoke("Ingrese la dirección de entrega a domicilio.", "Datos Incompletos", MessageBoxImage.Warning);
                return;
            }
        }

        MostrarPanelViajes = false;
        MostrarPanelPasajeros = false;
        MostrarPanelPago = true;
        MontoRecibido = TotalVenta;
        CalcularLiquidacion();
    }

    private void VolverAPasajeros()
    {
        MostrarPanelPago = false;
        MostrarPanelPasajeros = true;
        MostrarPanelViajes = false;
    }

    public async Task ConfirmarVentaAsync()
    {
        if (EsPagoEfectivo && FaltaDinero)
        {
            NotificarMensaje?.Invoke($"El monto recibido es insuficiente para completar la venta. Faltan S/. {DiferenciaFaltante:N2}.", "Pago Insuficiente", MessageBoxImage.Warning);
            return;
        }

        if (!EsSoloEncomienda && (ViajeSeleccionado == null || Pasajeros.Count == 0))
        {
            NotificarMensaje?.Invoke("Debe seleccionar un viaje y al menos 1 asiento para emitir boletos.", "Validación", MessageBoxImage.Warning);
            return;
        }

        if (EsSoloEncomienda && ViajeSeleccionado == null)
        {
            NotificarMensaje?.Invoke("Debe seleccionar un viaje para la encomienda.", "Validación", MessageBoxImage.Warning);
            return;
        }

        int viajeId = ViajeSeleccionado?.ViajeID ?? 0;
        int cajaTurnoId = Session?.CajaTurnoID > 0 ? Session.CajaTurnoID : 1;
        decimal totalVenta = TotalVenta;
        string metodoPago = MetodoPagoSeleccionado ?? "Efectivo";
        string? nroOp = string.IsNullOrWhiteSpace(NroOperacion) ? null : NroOperacion.Trim();
        bool esSoloEnc = EsSoloEncomienda;
        bool incEnc = IncluyeEncomienda;
        decimal montoRecibido = MontoRecibidoSeguro;
        decimal vuelto = Vuelto;

        var request = new RegistroVentaRequestDTO
        {
            ViajeId = viajeId,
            CajaTurnoId = cajaTurnoId,
            MetodoPago = metodoPago,
            NumeroOperacion = nroOp,
            MontoTotal = totalVenta
        };

        if (!esSoloEnc)
        {
            foreach (var p in Pasajeros)
            {
                request.Pasajeros.Add(new PasajeroDTO
                {
                    NroAsiento = p.NroAsiento,
                    Dni = p.Dni.Trim(),
                    Nombres = p.Nombres.Trim(),
                    Precio = p.Precio
                });
            }
        }

        string descEnc = "";
        decimal costoEnc = 0m;
        if (esSoloEnc || incEnc)
        {
            decimal pesoEnc;
            if (PresetSeleccionado?.EsPersonalizado == true)
            {
                descEnc = string.IsNullOrWhiteSpace(EncomiendaDescripcion) ? "Carga especial / Equipaje adicional" : EncomiendaDescripcion.Trim();
                pesoEnc = EncomiendaPesoKg > 0 ? EncomiendaPesoKg : 1.0m;
            }
            else
            {
                descEnc = !string.IsNullOrWhiteSpace(PresetSeleccionado?.Nombre)
                    ? PresetSeleccionado.Nombre
                    : (string.IsNullOrWhiteSpace(EncomiendaDescripcion) ? "Paquete / Encomienda" : EncomiendaDescripcion.Trim());
                pesoEnc = PresetSeleccionado != null && PresetSeleccionado.PesoRef > 0
                    ? PresetSeleccionado.PesoRef
                    : (EncomiendaPesoKg > 0 ? EncomiendaPesoKg : 3.0m);
            }
            costoEnc = EncomiendaCosto;

            string remTipoDoc = string.IsNullOrWhiteSpace(RemitenteTipoDoc) ? "DNI" : RemitenteTipoDoc;
            string? remDoc = string.IsNullOrWhiteSpace(RemitenteDoc) ? null : RemitenteDoc.Trim();
            string? remNombre = string.IsNullOrWhiteSpace(RemitenteNombre) ? null : RemitenteNombre.Trim();
            string? remTel = string.IsNullOrWhiteSpace(RemitenteTelefono) ? null : RemitenteTelefono.Trim();
            string destTipoDoc = string.IsNullOrWhiteSpace(DestinatarioTipoDoc) ? "DNI" : DestinatarioTipoDoc;
            string? destDoc = string.IsNullOrWhiteSpace(DestinatarioDoc) ? null : DestinatarioDoc.Trim();
            string? destNombre = string.IsNullOrWhiteSpace(DestinatarioNombre) ? null : DestinatarioNombre.Trim();
            string? destTel = string.IsNullOrWhiteSpace(DestinatarioTelefono) ? null : DestinatarioTelefono.Trim();

            if (!esSoloEnc && request.Pasajeros.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(remDoc)) remDoc = request.Pasajeros[0].Dni;
                if (string.IsNullOrWhiteSpace(remNombre)) remNombre = request.Pasajeros[0].Nombres;
                if (string.IsNullOrWhiteSpace(destDoc)) destDoc = request.Pasajeros[0].Dni;
                if (string.IsNullOrWhiteSpace(destNombre)) destNombre = request.Pasajeros[0].Nombres;
            }

            request.Encomienda = new EncomiendaDTO
            {
                Descripcion = descEnc,
                PesoKg = pesoEnc,
                CostoCarga = costoEnc,
                RemitenteTipoDoc = remTipoDoc,
                RemitenteDoc = remDoc,
                RemitenteNombre = remNombre,
                RemitenteTelefono = remTel,
                DestinatarioTipoDoc = destTipoDoc,
                DestinatarioDoc = destDoc,
                DestinatarioNombre = destNombre,
                DestinatarioTelefono = destTel,
                ModalidadEntrega = string.IsNullOrWhiteSpace(ModalidadEntrega) ? "Agencia" : ModalidadEntrega,
                DireccionEntrega = EsEntregaDomicilio && !string.IsNullOrWhiteSpace(DireccionEntrega) ? DireccionEntrega.Trim() : null,
                RecargoDelivery = RecargoDelivery,
                MetodoPago = metodoPago,
                NumeroOperacion = nroOp
            };
        }

        try
        {
            await _ventaService.RegistrarVentaAsync(request);

            SaldoCajaActual = await _cajaTurnoRepository.ObtenerSaldoActualAsync(cajaTurnoId);

            string detallePago = metodoPago == "Efectivo"
                ? $"Método de Pago: Efectivo (Entregado: S/. {montoRecibido:N2} | Vuelto: S/. {vuelto:N2})"
                : $"Método de Pago: {metodoPago}" + (string.IsNullOrWhiteSpace(nroOp) ? "" : $" (Ref: {nroOp})");

            string mensaje;
            if (esSoloEnc)
            {
                string modalidad = ModalidadEntrega == "Domicilio" ? "Entrega a Domicilio (+S/. 10.00)" : "Recojo en Agencia";
                mensaje = $"¡DESPACHO DE ENCOMIENDA CONFIRMADO CON ÉXITO!\n\nModalidad: {modalidad}\nTotal Pagado: S/. {totalVenta:N2}\nNuevo Saldo en Caja: S/. {SaldoCajaActual:N2}\n{detallePago}";
            }
            else
            {
                string asientos = string.Join("\n", request.Pasajeros.Select(p => $"• Asiento #{p.NroAsiento}: {p.Nombres} - DNI: {p.Dni} (S/. {p.Precio:N2})"));
                string cargaInfo = incEnc ? $"\n\nCarga / Encomienda Adjunta: {descEnc} (S/. {costoEnc:N2})" : "";
                mensaje = $"¡VENTA CONFIRMADA CON ÉXITO!\n\nBoletos Emitidos:\n{asientos}{cargaInfo}\n\nTotal Pagado: S/. {totalVenta:N2}\nNuevo Saldo en Caja: S/. {SaldoCajaActual:N2}\n{detallePago}";
            }

            NotificarMensaje?.Invoke(mensaje, "Emisión Exitosa", MessageBoxImage.Information);
            LimpiarBusqueda();
        }
        catch (AsientoNoDisponibleException ex)
        {
            if (ViajeSeleccionado != null)
            {
                await ConsultarAsientosAsync(ViajeSeleccionado.ViajeID);
            }

            NotificarMensaje?.Invoke(
                $"CONFLICTO DE CONCURRENCIA:\n\n{ex.Message}\n\nEl croquis de asientos ha sido actualizado.",
                "Asiento No Disponible",
                MessageBoxImage.Warning);
        }
        catch (CajaNoActivaException ex)
        {
            NotificarMensaje?.Invoke(
                $"ESTADO DE CAJA INVÁLIDO:\n\n{ex.Message}",
                "Caja No Activa",
                MessageBoxImage.Error);
        }
        catch (VentaValidationException ex)
        {
            NotificarMensaje?.Invoke(
                $"ERROR DE VALIDACIÓN:\n\n{ex.Message}",
                "Validación de Venta",
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (ViajeSeleccionado != null)
            {
                await ConsultarAsientosAsync(ViajeSeleccionado.ViajeID);
            }

            NotificarMensaje?.Invoke(
                $"TRANSACCIÓN ABORTADA - ROLLBACK EJECUTADO:\n\n{ex.Message}\n\n" +
                "Garantía ACID (Atomicidad):\n" +
                "• La transacción se revirtió íntegramente en la base de datos.\n" +
                "• No se emitieron boletos ni se registraron encomiendas.\n" +
                "• La caja del turno permaneció intacta sin descuadre de dinero.\n" +
                "• El estado de los asientos se ha sincronizado en tiempo real.",
                "Rollback de Seguridad - Transacción ACID",
                MessageBoxImage.Warning);
        }
    }

    private void AplicarMontoRapido(string? valor)
    {
        if (valor == "Exacto")
        {
            MontoRecibido = TotalVenta;
        }
        else if (valor != null && valor.StartsWith("+") && decimal.TryParse(valor.Substring(1), out decimal suma))
        {
            MontoRecibido = (MontoRecibido ?? TotalVenta) + suma;
        }
        else if (decimal.TryParse(valor, out decimal montoFijo))
        {
            MontoRecibido = montoFijo;
        }
    }

    private void CalcularLiquidacion()
    {
        OnPropertyChanged(nameof(TotalBoletos));
        OnPropertyChanged(nameof(TotalVenta));
        OnPropertyChanged(nameof(EncomiendaCosto));
        OnPropertyChanged(nameof(RecargoDelivery));
        OnPropertyChanged(nameof(Vuelto));
        OnPropertyChanged(nameof(FaltaDinero));
        OnPropertyChanged(nameof(DiferenciaFaltante));
        OnPropertyChanged(nameof(PuedeContinuarAPasajeros));
        OnPropertyChanged(nameof(TextoBotonContinuar));
        OnPropertyChanged(nameof(TextoBotonContinuarAPago));
        OnPropertyChanged(nameof(TextoBotonConfirmarPago));
        OnPropertyChanged(nameof(TextoBotonFooterConfirmar));
        OnPropertyChanged(nameof(TextoBotonVolverDePago));
        OnPropertyChanged(nameof(TextoBotonVolverDePaso2));
        CommandManager.InvalidateRequerySuggested();
    }
}
