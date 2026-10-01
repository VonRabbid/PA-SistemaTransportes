using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Common;

namespace SistemaTransportes
{
    public partial class VentaBoletosWindow : Window
    {
        private static readonly Regex SoloNumerosRegex = new("^[0-9]+$", RegexOptions.Compiled);

        public VentaBoletosWindow(UsuarioSessionModel? session = null)
        {
            InitializeComponent();
            DataContext = new VentaIntegradaViewModel(this, session);
        }

        private void NumeroOperacion_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !SoloNumerosRegex.IsMatch(e.Text);
        }

        private void NumeroOperacion_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (string.IsNullOrEmpty(text) || !SoloNumerosRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var login = new MainWindow();
            Application.Current.MainWindow = login;
            login.Show();
            this.Close();
        }

        private void TxtMontoRecibido_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TxtMontoRecibido_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }

    #region ViewModels Auxiliares

    public class UsuarioSessionModel
    {
        public int UsuarioID { get; set; }
        public string Username { get; set; } = "OpControl";
        public string Rol { get; set; } = "Operador";
        public int CajaTurnoID { get; set; } = 1;
        public string Nombres { get; set; } = "Operador de Control";
    }

    public class AsientoItemViewModel : ViewModelBase
    {
        public int NroAsiento { get; set; }
        public int Piso { get; set; } = 1;

        private string _estado = "Libre";
        public string Estado
        {
            get => _estado;
            set
            {
                if (SetProperty(ref _estado, value))
                {
                    OnPropertyChanged(nameof(EstadoVisual));
                    OnPropertyChanged(nameof(EsSeleccionado));
                    OnPropertyChanged(nameof(EsClickeable));
                }
            }
        }

        public string EstadoVisual => Estado;
        public bool EsSeleccionado => Estado == "Seleccionado";
        public bool EsClickeable => Estado != "Ocupado";

        public ICommand? ClickCommand { get; set; }
    }

    public class FilaAsientoItemViewModel
    {
        public AsientoItemViewModel? AsientoVentanaIzq { get; set; }
        public AsientoItemViewModel? AsientoPasilloIzq { get; set; }
        public AsientoItemViewModel? AsientoPasilloDer { get; set; }
        public AsientoItemViewModel? AsientoVentanaDer { get; set; }
    }

    public class PasajeroItemViewModel : ViewModelBase
    {
        public int NroAsiento { get; set; }
        public int Piso { get; set; } = 1;
        public decimal Precio { get; set; }

        private string _dni = "";
        public string Dni
        {
            get => _dni;
            set => SetProperty(ref _dni, value);
        }

        private string _nombres = "";
        public string Nombres
        {
            get => _nombres;
            set => SetProperty(ref _nombres, value);
        }
    }

    public class PresetItemViewModel : ViewModelBase
    {
        private decimal _tarifa;
        private decimal _tarifaPorKgActual = 3.00m;

        public string Titulo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal TarifaPasajero { get; set; }
        public decimal TarifaSoloEncomienda { get; set; }

        public decimal Tarifa
        {
            get => _tarifa;
            set
            {
                if (SetProperty(ref _tarifa, value))
                {
                    OnPropertyChanged(nameof(TarifaDisplay));
                    OnPropertyChanged(nameof(DisplayTexto));
                }
            }
        }

        public decimal PesoRef { get; set; }
        public bool EsPersonalizado { get; set; }
        public string Icono { get; set; } = "📦";

        public decimal TarifaPorKgActual
        {
            get => _tarifaPorKgActual;
            set => SetProperty(ref _tarifaPorKgActual, value);
        }

        public string TarifaDisplay => EsPersonalizado
            ? $"S/. {TarifaPorKgActual:N2} / Kg"
            : $"S/. {Tarifa:N2}";

        public string PesoRefDisplay => EsPersonalizado
            ? "Balanza manual (máx. 50 kg)"
            : $"{PesoRef:0.#} kg ref.";

        public string DisplayTexto => EsPersonalizado
            ? $"{Icono} {Nombre} — S/. {TarifaPorKgActual:N2} / Kg"
            : $"{Icono} {Nombre} — S/. {Tarifa:N2} ({PesoRef:0.#} kg ref.)";

        public void ActualizarModo(bool esSoloEncomienda)
        {
            TarifaPorKgActual = esSoloEncomienda ? 4.00m : 3.00m;
            Tarifa = esSoloEncomienda ? TarifaSoloEncomienda : TarifaPasajero;
            OnPropertyChanged(nameof(TarifaDisplay));
            OnPropertyChanged(nameof(DisplayTexto));
        }

        public bool CoincideCon(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string t = texto.Trim();

            return string.Equals(Nombre, t, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Titulo, t, StringComparison.OrdinalIgnoreCase)
                || Nombre.StartsWith(t, StringComparison.OrdinalIgnoreCase)
                || Titulo.StartsWith(t, StringComparison.OrdinalIgnoreCase)
                || Nombre.Contains(t, StringComparison.OrdinalIgnoreCase)
                || Titulo.Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => DisplayTexto;
    }

    public class ViajeItemViewModel : ViewModelBase
    {
        public int ViajeID { get; set; }
        public string Origen { get; set; } = "Lima";
        public string Destino { get; set; } = "Huancayo";
        public string BusPlaca { get; set; } = "ABC-123";
        public string PlacaBus => BusPlaca;
        public string TipoServicio { get; set; } = "Servicio Directo";
        public string Categoria { get; set; } = "VIP";
        public string HoraSalidaTexto { get; set; } = "08:00 AM";
        public string HoraLlegadaTexto { get; set; } = "03:30 PM";
        public string Duracion { get; set; } = "07h 30m";
        public DateTime FechaHoraSalida { get; set; } = DateTime.Today.AddHours(8);
        public DateTime FechaHoraLlegada { get; set; } = DateTime.Today.AddHours(15).AddMinutes(30);
        public decimal PrecioBase { get; set; } = 65.00m;

        public bool EsIdaYVuelta { get; set; }
        public string EtiquetaTarifa => EsIdaYVuelta ? "Por Pasajero (Ida y Vuelta)" : "Por Pasajero";

        public bool SalidaVencida => FechaHoraSalida < DateTime.Now;
        public bool SalidaDisponible => !SalidaVencida;

        public string TextoBotonAccion
        {
            get
            {
                if (SalidaVencida)
                    return "Horario no disponible";

                // Respeta si es solo encomienda o venta de pasaje
                return "Comprar";
            }
        }

        public string RutaTexto => $"{Origen} ➔ {Destino}";
        public string SalidaCompletaTexto => $"Salida: {HoraSalidaTexto} ({RutaTexto})";
    }

    #endregion

    #region ViewModel Principal de Venta Integrada

    public class VentaIntegradaViewModel : ViewModelBase
    {
        private readonly Window _window;

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
                    CargarDestinosPorOrigen(value);
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

                    // Si la fecha de vuelta previa queda fuera de la ventana de 30 días, se resetea
                    if (FechaVuelta.HasValue && (FechaVuelta.Value < valor || FechaVuelta.Value > FechaVueltaMaxima))
                    {
                        FechaVuelta = null;
                    }

                    if (MostrarResultadosViajes)
                    {
                        BuscarViajes();
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
                        BuscarViajes();
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

        public VentaIntegradaViewModel(Window window, UsuarioSessionModel? session = null)
        {
            _window = window;
            if (session != null)
            {
                Session = session;
            }

            _incluyeEncomienda = false;
            _presetSeleccionado = null;
            _encomiendaCosto = 0m;
            _encomiendaPesoKg = 0m;
            _encomiendaPesoKgTexto = "0";
            _encomiendaDescripcion = string.Empty;
            _esCargaPersonalizada = false;

            BuscarViajesCommand = new RelayCommand(BuscarViajes);
            LimpiarBusquedaCommand = new RelayCommand(LimpiarBusqueda);
            IntercambiarCiudadesCommand = new RelayCommand(IntercambiarCiudades);
            OrdenarPorSalidaCommand = new RelayCommand(() => OrdenarViajes(v => v.FechaHoraSalida));
            OrdenarPorPrecioCommand = new RelayCommand(() => OrdenarViajes(v => v.PrecioBase));
            SeleccionarViajeCommand = new RelayCommand<ViajeItemViewModel>(SeleccionarViaje);
            CambiarPisoCommand = new RelayCommand<object>(p => CambiarPiso(Convert.ToInt32(p)));
            IrAPasajerosCommand = new RelayCommand(IrAPasajeros, () => PuedeContinuarAPasajeros);
            VolverAViajesCommand = new RelayCommand(VolverAViajes);
            IrAPagoCommand = new RelayCommand(IrAPago);
            VolverAPasajerosCommand = new RelayCommand(VolverAPasajeros);
            ConfirmarVentaFinalCommand = new RelayCommand(ConfirmarVenta);
            CerrarSesionCommand = new RelayCommand(CerrarSesion);
            RefrescarMapaCommand = new RelayCommand(RefrescarMapa);
            MontoRapidoCommand = new RelayCommand<string>(AplicarMontoRapido);

            _ = InicializarDatosDesdeBdAsync();
        }

        private static string ObtenerCadenaConexion()
        {
            return ConfigurationManager.ConnectionStrings["BD_Transportes"]?.ConnectionString
                ?? "Server=tcp:sistema-transportes-2026.database.windows.net,1433;Initial Catalog=BD_Transportes;Persist Security Info=False;User ID=admin_st;Password=1425PA31%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        private async Task InicializarDatosDesdeBdAsync()
        {
            try
            {
                string connectionString = ObtenerCadenaConexion();

                // 1. Saldo Real de Caja (dbo.CajasTurno)
                if (Session.CajaTurnoID > 0)
                {
                    await Task.Run(() =>
                    {
                        using var connection = new SqlConnection(connectionString);
                        connection.Open();

                        const string queryCaja = @"
                            SELECT MontoActual 
                            FROM dbo.CajasTurno 
                            WHERE CajaTurnoID = @CajaTurnoID;";

                        using var cmdCaja = new SqlCommand(queryCaja, connection);
                        cmdCaja.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = Session.CajaTurnoID;

                        var result = cmdCaja.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            decimal saldo = Convert.ToDecimal(result);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SaldoCajaActual = saldo;
                            });
                        }
                    });
                }

                // 2. Catálogo Oficial de Ciudades de Origen (dbo.Viajes)
                await Task.Run(() =>
                {
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();

                    const string queryOrigenes = @"
                        SELECT DISTINCT Origen 
                        FROM dbo.Viajes 
                        ORDER BY Origen ASC;";

                    using var cmd = new SqlCommand(queryOrigenes, connection);
                    using var reader = cmd.ExecuteReader();

                    var listaOrigenes = new List<string>();
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            listaOrigenes.Add(reader.GetString(0));
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OrigenesDisponibles.Clear();
                        foreach (var orig in listaOrigenes)
                        {
                            OrigenesDisponibles.Add(orig);
                        }

                        OrigenSeleccionado = null;
                        DestinoSeleccionado = null;
                        MostrarResultadosViajes = false;
                    });
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error al cargar datos desde SQL Server:\n{ex.Message}",
                                    "Error de Conexión BD",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                });
            }
        }

        private void CargarDestinosPorOrigen(string? origen)
        {
            DestinosDisponibles.Clear();
            if (string.IsNullOrWhiteSpace(origen))
            {
                DestinoSeleccionado = null;
                return;
            }

            try
            {
                string connectionString = ObtenerCadenaConexion();
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                const string queryDestinos = @"
                    SELECT DISTINCT Destino 
                    FROM dbo.Viajes 
                    WHERE Origen = @Origen 
                    ORDER BY Destino ASC;";

                using var cmd = new SqlCommand(queryDestinos, connection);
                cmd.Parameters.Add("@Origen", SqlDbType.NVarChar, 50).Value = origen;

                using var reader = cmd.ExecuteReader();
                var listaDestinos = new List<string>();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        listaDestinos.Add(reader.GetString(0));
                    }
                }

                foreach (var d in listaDestinos)
                {
                    DestinosDisponibles.Add(d);
                }

                if (DestinosDisponibles.Count > 0)
                {
                    DestinoSeleccionado = DestinosDisponibles[0];
                }
                else
                {
                    DestinoSeleccionado = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar destinos para {origen}: {ex.Message}");
            }
        }

        private void BuscarViajes()
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
                string connectionString = ObtenerCadenaConexion();
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                const string queryViajes = @"
                    SELECT v.ViajeID, v.Origen, v.Destino, v.TipoServicio, v.FechaSalida, 
                           v.FechaHoraLlegada, v.Categoria, v.DuracionEstimada, v.PrecioBase, b.Placa
                    FROM dbo.Viajes v
                    INNER JOIN dbo.Buses b ON v.BusID = b.BusID
                    WHERE (@Origen IS NULL OR v.Origen = @Origen)
                      AND (@Destino IS NULL OR v.Destino = @Destino)
                    ORDER BY v.FechaSalida ASC;";

                using var cmd = new SqlCommand(queryViajes, connection);
                cmd.Parameters.Add("@Origen", SqlDbType.NVarChar, 50).Value = (object?)origen ?? DBNull.Value;
                cmd.Parameters.Add("@Destino", SqlDbType.NVarChar, 50).Value = (object?)destino ?? DBNull.Value;

                bool esIdaYVuelta = FechaVuelta.HasValue;
                // Factor promocional de retorno: 1.75x (75% adicional por el viaje de vuelta, otorgando 25% de descuento en el tramo de regreso)
                decimal factorTarifa = esIdaYVuelta ? 1.75m : 1.00m;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int viajeId = reader.GetInt32(0);
                    string orig = reader.GetString(1);
                    string dest = reader.GetString(2);
                    string tipoServicio = reader.IsDBNull(3) ? "Directo" : reader.GetString(3);
                    DateTime fechaSalidaDb = reader.GetDateTime(4);
                    DateTime? fechaLlegadaDb = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
                    string categoria = reader.IsDBNull(6) ? "Clásico" : reader.GetString(6);
                    string duracionEstimada = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    decimal precioBase = reader.GetDecimal(8);
                    string placa = reader.GetString(9);

                    decimal precioCalculado = Math.Round(precioBase * factorTarifa, 2);

                    DateTime fechaSalidaAjustada = new DateTime(
                        fechaBase.Year, fechaBase.Month, fechaBase.Day,
                        fechaSalidaDb.Hour, fechaSalidaDb.Minute, fechaSalidaDb.Second);

                    TimeSpan duracionSpan = fechaLlegadaDb.HasValue
                        ? (fechaLlegadaDb.Value - fechaSalidaDb)
                        : TimeSpan.FromHours(7.5);

                    DateTime fechaLlegadaAjustada = fechaSalidaAjustada.Add(duracionSpan);

                    string horaSalidaTexto = fechaSalidaAjustada.ToString("hh:mm tt", CultureInfo.InvariantCulture).ToUpper();
                    string horaLlegadaTexto = fechaLlegadaAjustada.ToString("hh:mm tt", CultureInfo.InvariantCulture).ToUpper();

                    string duracion = string.IsNullOrWhiteSpace(duracionEstimada)
                        ? $"{(int)duracionSpan.TotalHours:00}h {duracionSpan.Minutes:00}m"
                        : duracionEstimada;

                    ViajesDisponibles.Add(new ViajeItemViewModel
                    {
                        ViajeID = viajeId,
                        Origen = orig,
                        Destino = dest,
                        BusPlaca = placa,
                        TipoServicio = tipoServicio,
                        Categoria = categoria,
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
                MessageBox.Show($"Error al buscar viajes en la base de datos:\n{ex.Message}",
                                "Error de Búsqueda",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
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

        private void SeleccionarViaje(ViajeItemViewModel? viaje)
        {
            if (viaje == null) return;
            if (viaje.SalidaVencida)
            {
                MessageBox.Show(
                    $"El bus programado para las {viaje.HoraSalidaTexto} ya partió o su horario de venta está cerrado.",
                    "Salida no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ViajeSeleccionado = viaje;
            MostrarMapaAsientos = true;
            AsientosSeleccionados.Clear();

            if (!EsSoloEncomienda)
            {
                ConsultarAsientosDesdeBd(viaje.ViajeID);
            }
            else
            {
                CalcularLiquidacion();
            }
        }

        private void ConsultarAsientosDesdeBd(int viajeId)
        {
            CargandoAsientos = true;
            _todosLosAsientos.Clear();

            try
            {
                string connectionString = ObtenerCadenaConexion();
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                const string queryAsientos = @"
                    SELECT a.NroAsiento, a.Piso, ISNULL(eav.Estado, 'Libre') AS Estado
                    FROM dbo.Asientos a
                    INNER JOIN dbo.Viajes v ON a.BusID = v.BusID
                    LEFT JOIN dbo.EstadoAsientosViaje eav ON eav.ViajeID = v.ViajeID AND eav.NroAsiento = a.NroAsiento
                    WHERE v.ViajeID = @ViajeID
                    ORDER BY a.Piso ASC, a.NroAsiento ASC;";

                using var cmd = new SqlCommand(queryAsientos, connection);
                cmd.Parameters.Add("@ViajeID", SqlDbType.Int).Value = viajeId;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int nroAsiento = reader.GetInt32(0);
                    int piso = reader.GetInt32(1);
                    string estado = reader.GetString(2);

                    var asientoItem = new AsientoItemViewModel
                    {
                        NroAsiento = nroAsiento,
                        Piso = piso,
                        Estado = estado
                    };
                    asientoItem.ClickCommand = new RelayCommand(() => ToggleAsiento(asientoItem));
                    _todosLosAsientos.Add(asientoItem);
                }

                _pisoActual = 1;
                CargarAsientos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar croquis de asientos del bus:\n{ex.Message}",
                                "Error de Asientos",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
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
                MessageBox.Show(
                    "El modo 'Solo Envío de Encomienda' está activo. La carga viaja en bodega y no requiere selección de asientos.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show("Puede seleccionar un máximo de 5 asientos por operación.", "Límite Alcanzado", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                asiento.Estado = "Seleccionado";
                AsientosSeleccionados.Add(asiento);
            }

            CalcularLiquidacion();
        }

        private void RefrescarMapa()
        {
            if (ViajeSeleccionado != null)
            {
                ConsultarAsientosDesdeBd(ViajeSeleccionado.ViajeID);
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
                    MessageBox.Show("Seleccione un viaje de destino para la encomienda.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("Debe seleccionar al menos 1 asiento para continuar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var pas in Pasajeros)
                {
                    if (string.IsNullOrWhiteSpace(pas.Dni) || pas.Dni.Trim().Length < 8)
                    {
                        MessageBox.Show($"Ingrese un DNI válido de 8 dígitos para el Asiento N° {pas.NroAsiento}.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(pas.Nombres) || pas.Nombres.Trim().Length < 3)
                    {
                        MessageBox.Show($"Ingrese los nombres completos para el Asiento N° {pas.NroAsiento}.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            else
            {
                if (ViajeSeleccionado == null)
                {
                    MessageBox.Show("Seleccione un viaje para la encomienda.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RemitenteDoc) || string.IsNullOrWhiteSpace(RemitenteNombre))
                {
                    MessageBox.Show("Ingrese los datos del remitente.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(DestinatarioDoc) || string.IsNullOrWhiteSpace(DestinatarioNombre))
                {
                    MessageBox.Show("Ingrese los datos del destinatario.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (EsEntregaDomicilio && string.IsNullOrWhiteSpace(DireccionEntrega))
                {
                    MessageBox.Show("Ingrese la dirección de entrega a domicilio.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private async void ConfirmarVenta()
        {
            if (EsPagoEfectivo && FaltaDinero)
            {
                MessageBox.Show($"El monto recibido es insuficiente para completar la venta. Faltan S/. {DiferenciaFaltante:N2}.", "Pago Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EsSoloEncomienda && (ViajeSeleccionado == null || Pasajeros.Count == 0))
            {
                MessageBox.Show("Debe seleccionar un viaje y al menos 1 asiento para emitir boletos.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EsSoloEncomienda && ViajeSeleccionado == null)
            {
                MessageBox.Show("Debe seleccionar un viaje para la encomienda.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var listaPasajeros = Pasajeros.Select(p => new
            {
                p.NroAsiento,
                p.Piso,
                Dni = p.Dni.Trim(),
                Nombres = p.Nombres.Trim(),
                p.Precio
            }).ToList();

            string descEnc;
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
            decimal costoEnc = EncomiendaCosto;
            string remTipoDoc = string.IsNullOrWhiteSpace(RemitenteTipoDoc) ? "DNI" : RemitenteTipoDoc;
            string? remDoc = string.IsNullOrWhiteSpace(RemitenteDoc) ? null : RemitenteDoc.Trim();
            string? remNombre = string.IsNullOrWhiteSpace(RemitenteNombre) ? null : RemitenteNombre.Trim();
            string? remTel = string.IsNullOrWhiteSpace(RemitenteTelefono) ? null : RemitenteTelefono.Trim();
            string destTipoDoc = string.IsNullOrWhiteSpace(DestinatarioTipoDoc) ? "DNI" : DestinatarioTipoDoc;
            string? destDoc = string.IsNullOrWhiteSpace(DestinatarioDoc) ? null : DestinatarioDoc.Trim();
            string? destNombre = string.IsNullOrWhiteSpace(DestinatarioNombre) ? null : DestinatarioNombre.Trim();
            string? destTel = string.IsNullOrWhiteSpace(DestinatarioTelefono) ? null : DestinatarioTelefono.Trim();

            if (!esSoloEnc && listaPasajeros.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(remDoc)) remDoc = listaPasajeros[0].Dni;
                if (string.IsNullOrWhiteSpace(remNombre)) remNombre = listaPasajeros[0].Nombres;
                if (string.IsNullOrWhiteSpace(destDoc)) destDoc = listaPasajeros[0].Dni;
                if (string.IsNullOrWhiteSpace(destNombre)) destNombre = listaPasajeros[0].Nombres;
            }

            string modEntrega = string.IsNullOrWhiteSpace(ModalidadEntrega) ? "Agencia" : ModalidadEntrega;
            string? dirEntrega = EsEntregaDomicilio && !string.IsNullOrWhiteSpace(DireccionEntrega) ? DireccionEntrega.Trim() : null;
            decimal recDelivery = RecargoDelivery;

            try
            {
                await Task.Run(() =>
                {
                    using var connection = new SqlConnection(ObtenerCadenaConexion());
                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    try
                    {
                        int? primerBoletoId = null;

                        if (!esSoloEnc)
                        {
                            const string sqlActualizarAsiento = @"
                                UPDATE dbo.EstadoAsientosViaje 
                                SET Estado = 'Ocupado' 
                                WHERE ViajeID = @ViajeID AND NroAsiento = @NroAsiento AND Estado = 'Libre';";

                            const string sqlInsertBoleto = @"
                                INSERT INTO dbo.Boletos (
                                    ViajeID, NroAsiento, DniPasajero, NombrePasajero, 
                                    PrecioFinal, FechaEmision, CajaTurnoID, MetodoPago, NumeroOperacion
                                ) 
                                VALUES (
                                    @ViajeID, @NroAsiento, @DniPasajero, @NombrePasajero, 
                                    @PrecioFinal, GETDATE(), @CajaTurnoID, @MetodoPago, @NumeroOperacion
                                );
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            foreach (var pas in listaPasajeros)
                            {
                                using (var cmdAsiento = new SqlCommand(sqlActualizarAsiento, connection, transaction))
                                {
                                    cmdAsiento.Parameters.Add("@ViajeID", SqlDbType.Int).Value = viajeId;
                                    cmdAsiento.Parameters.Add("@NroAsiento", SqlDbType.Int).Value = pas.NroAsiento;
                                    int filasModificadas = cmdAsiento.ExecuteNonQuery();
                                    if (filasModificadas == 0)
                                    {
                                        throw new InvalidOperationException($"El Asiento #{pas.NroAsiento} ya no está disponible. Fue adquirido simultáneamente por otro operador en el sistema.");
                                    }
                                }

                                using (var cmdBoleto = new SqlCommand(sqlInsertBoleto, connection, transaction))
                                {
                                    cmdBoleto.Parameters.Add("@ViajeID", SqlDbType.Int).Value = viajeId;
                                    cmdBoleto.Parameters.Add("@NroAsiento", SqlDbType.Int).Value = pas.NroAsiento;
                                    cmdBoleto.Parameters.Add("@DniPasajero", SqlDbType.NVarChar, 8).Value = pas.Dni;
                                    cmdBoleto.Parameters.Add("@NombrePasajero", SqlDbType.NVarChar, 100).Value = pas.Nombres;

                                    var pPrecio = cmdBoleto.Parameters.Add("@PrecioFinal", SqlDbType.Decimal);
                                    pPrecio.Precision = 18;
                                    pPrecio.Scale = 2;
                                    pPrecio.Value = pas.Precio;

                                    cmdBoleto.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = cajaTurnoId;
                                    cmdBoleto.Parameters.Add("@MetodoPago", SqlDbType.VarChar, 30).Value = metodoPago;
                                    cmdBoleto.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 50).Value = (object?)nroOp ?? DBNull.Value;

                                    object? resId = cmdBoleto.ExecuteScalar();
                                    if (primerBoletoId == null && resId != null && resId != DBNull.Value)
                                    {
                                        primerBoletoId = Convert.ToInt32(resId);
                                    }
                                }
                            }
                        }

                        if (esSoloEnc || incEnc)
                        {
                            const string sqlInsertEncomienda = @"
                                INSERT INTO dbo.Encomiendas (
                                    BoletoID, ViajeID, CajaTurnoID, Descripcion, PesoKg, CostoCarga, 
                                    FechaRecepcion, RemitenteTipoDoc, RemitenteDoc, RemitenteNombre, RemitenteTelefono,
                                    DestinatarioTipoDoc, DestinatarioDoc, DestinatarioNombre, DestinatarioTelefono,
                                    ModalidadEntrega, DireccionEntrega, RecargoDelivery, MetodoPago, NumeroOperacion
                                ) 
                                VALUES (
                                    @BoletoID, @ViajeID, @CajaTurnoID, @Descripcion, @PesoKg, @CostoCarga, 
                                    GETDATE(), @RemitenteTipoDoc, @RemitenteDoc, @RemitenteNombre, @RemitenteTelefono,
                                    @DestinatarioTipoDoc, @DestinatarioDoc, @DestinatarioNombre, @DestinatarioTelefono,
                                    @ModalidadEntrega, @DireccionEntrega, @RecargoDelivery, @MetodoPago, @NumeroOperacion
                                );";

                            using var cmdEnc = new SqlCommand(sqlInsertEncomienda, connection, transaction);
                            cmdEnc.Parameters.Add("@BoletoID", SqlDbType.Int).Value = (primerBoletoId.HasValue && primerBoletoId.Value > 0) ? (object)primerBoletoId.Value : DBNull.Value;
                            cmdEnc.Parameters.Add("@ViajeID", SqlDbType.Int).Value = viajeId > 0 ? (object)viajeId : DBNull.Value;
                            cmdEnc.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = cajaTurnoId;
                            cmdEnc.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 150).Value = descEnc;

                            var pPeso = cmdEnc.Parameters.Add("@PesoKg", SqlDbType.Decimal);
                            pPeso.Precision = 10;
                            pPeso.Scale = 2;
                            pPeso.Value = pesoEnc;

                            var pCosto = cmdEnc.Parameters.Add("@CostoCarga", SqlDbType.Decimal);
                            pCosto.Precision = 18;
                            pCosto.Scale = 2;
                            pCosto.Value = costoEnc;

                            cmdEnc.Parameters.Add("@RemitenteTipoDoc", SqlDbType.VarChar, 10).Value = remTipoDoc;
                            cmdEnc.Parameters.Add("@RemitenteDoc", SqlDbType.VarChar, 15).Value = (object?)remDoc ?? DBNull.Value;
                            cmdEnc.Parameters.Add("@RemitenteNombre", SqlDbType.VarChar, 120).Value = (object?)remNombre ?? DBNull.Value;
                            cmdEnc.Parameters.Add("@RemitenteTelefono", SqlDbType.VarChar, 15).Value = (object?)remTel ?? DBNull.Value;

                            cmdEnc.Parameters.Add("@DestinatarioTipoDoc", SqlDbType.VarChar, 10).Value = destTipoDoc;
                            cmdEnc.Parameters.Add("@DestinatarioDoc", SqlDbType.VarChar, 15).Value = (object?)destDoc ?? DBNull.Value;
                            cmdEnc.Parameters.Add("@DestinatarioNombre", SqlDbType.VarChar, 120).Value = (object?)destNombre ?? DBNull.Value;
                            cmdEnc.Parameters.Add("@DestinatarioTelefono", SqlDbType.VarChar, 15).Value = (object?)destTel ?? DBNull.Value;

                            cmdEnc.Parameters.Add("@ModalidadEntrega", SqlDbType.VarChar, 30).Value = modEntrega;
                            cmdEnc.Parameters.Add("@DireccionEntrega", SqlDbType.VarChar, 200).Value = (object?)dirEntrega ?? DBNull.Value;

                            var pRecargo = cmdEnc.Parameters.Add("@RecargoDelivery", SqlDbType.Decimal);
                            pRecargo.Precision = 10;
                            pRecargo.Scale = 2;
                            pRecargo.Value = recDelivery;

                            cmdEnc.Parameters.Add("@MetodoPago", SqlDbType.VarChar, 30).Value = metodoPago;
                            cmdEnc.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 50).Value = (object?)nroOp ?? DBNull.Value;

                            cmdEnc.ExecuteNonQuery();
                        }

                        const string sqlActualizarCaja = @"
                            UPDATE dbo.CajasTurno 
                            SET MontoActual = MontoActual + @TotalVenta 
                            WHERE CajaTurnoID = @CajaTurnoID;";

                        using (var cmdCaja = new SqlCommand(sqlActualizarCaja, connection, transaction))
                        {
                            var pTotal = cmdCaja.Parameters.Add("@TotalVenta", SqlDbType.Decimal);
                            pTotal.Precision = 18;
                            pTotal.Scale = 2;
                            pTotal.Value = totalVenta;

                            cmdCaja.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = cajaTurnoId;

                            cmdCaja.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });

                SaldoCajaActual += totalVenta;

                string detallePago = metodoPago == "Efectivo"
                    ? $"Método de Pago: Efectivo (Entregado: S/. {montoRecibido:N2} | Vuelto: S/. {vuelto:N2})"
                    : $"Método de Pago: {metodoPago}" + (string.IsNullOrWhiteSpace(nroOp) ? "" : $" (Ref: {nroOp})");

                string mensaje;
                if (esSoloEnc)
                {
                    string modalidad = modEntrega == "Domicilio" ? "Entrega a Domicilio (+S/. 10.00)" : "Recojo en Agencia";
                    mensaje = $"¡DESPACHO DE ENCOMIENDA CONFIRMADO CON ÉXITO!\n\nModalidad: {modalidad}\nTotal Pagado: S/. {totalVenta:N2}\nNuevo Saldo en Caja: S/. {SaldoCajaActual:N2}\n{detallePago}";
                }
                else
                {
                    string asientos = string.Join("\n", listaPasajeros.Select(p => $"• Asiento #{p.NroAsiento} (Piso {p.Piso}): {p.Nombres} - DNI: {p.Dni} (S/. {p.Precio:N2})"));
                    string cargaInfo = incEnc ? $"\n\nCarga / Encomienda Adjunta: {descEnc} (S/. {costoEnc:N2})" : "";
                    mensaje = $"¡VENTA CONFIRMADA CON ÉXITO!\n\nBoletos Emitidos:\n{asientos}{cargaInfo}\n\nTotal Pagado: S/. {totalVenta:N2}\nNuevo Saldo en Caja: S/. {SaldoCajaActual:N2}\n{detallePago}";
                }

                MessageBox.Show(mensaje, "Emisión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarBusqueda();
            }
            catch (Exception ex)
            {
                // En caso de conflicto de concurrencia o rollback, refrescamos el croquis de asientos en tiempo real
                if (ViajeSeleccionado != null)
                {
                    ConsultarAsientosDesdeBd(ViajeSeleccionado.ViajeID);
                }

                MessageBox.Show(
                    $"TRANSACCIÓN ABORTADA - ROLLBACK EJECUTADO:\n\n{ex.Message}\n\n" +
                    "Garantía ACID (Atomicidad):\n" +
                    "• La transacción en SQL Server se revirtió íntegramente (Rollback).\n" +
                    "• No se emitieron boletos ni se registraron encomiendas.\n" +
                    "• La caja del turno permaneció intacta sin descuadre de dinero.\n" +
                    "• El estado de los asientos se ha sincronizado en tiempo real.",
                    "Rollback de Seguridad - Transacción ACID",
                    MessageBoxButton.OK,
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

        private void CerrarSesion()
        {
            var login = new MainWindow();
            Application.Current.MainWindow = login;
            login.Show();
            _window.Close();
        }
    }

    #endregion
}
