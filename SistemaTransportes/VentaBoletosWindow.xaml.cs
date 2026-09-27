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

    public class PresetItemViewModel
    {
        public string Nombre { get; set; } = "";
        public string Icono { get; set; } = "Box";
        public string Descripcion { get; set; } = "";
        public string TarifaDisplay { get; set; } = "";
        public decimal Tarifa { get; set; }
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

        public DateTime? FechaIda { get; set; } = DateTime.Today;
        public DateTime FechaMinima { get; } = DateTime.Today;
        public DateTime? FechaVuelta { get; set; }
        public DateTime FechaVueltaMinima { get; } = DateTime.Today;

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
                    CalcularLiquidacion();
                }
            }
        }

        // --- Modalidad y Carga ---
        private bool _esModoPasajeConEquipaje = true;
        public bool EsModoPasajeConEquipaje
        {
            get => _esModoPasajeConEquipaje;
            set
            {
                if (SetProperty(ref _esModoPasajeConEquipaje, value))
                {
                    if (value)
                    {
                        EsSoloEncomienda = false;
                    }
                    OnPropertyChanged(nameof(EsSoloEncomienda));
                    CalcularLiquidacion();
                }
            }
        }

        private bool _esSoloEncomienda;
        public bool EsSoloEncomienda
        {
            get => _esSoloEncomienda;
            set
            {
                if (SetProperty(ref _esSoloEncomienda, value))
                {
                    if (value)
                    {
                        EsModoPasajeConEquipaje = false;
                        MostrarMapaAsientos = false;
                    }
                    OnPropertyChanged(nameof(EsModoPasajeConEquipaje));
                    CalcularLiquidacion();
                }
            }
        }

        public bool IncluyeEncomienda { get; set; }

        public ObservableCollection<PresetItemViewModel> PresetsDisponibles { get; } = new()
        {
            new PresetItemViewModel { Nombre = "Sobre / Doc", Icono = "Envelope", Descripcion = "Hasta 1 Kg", Tarifa = 15.00m, TarifaDisplay = "S/. 15.00" },
            new PresetItemViewModel { Nombre = "Paquete Chico", Icono = "Box", Descripcion = "Hasta 5 Kg", Tarifa = 25.00m, TarifaDisplay = "S/. 25.00" },
            new PresetItemViewModel { Nombre = "Caja Mediana", Icono = "BoxesStacked", Descripcion = "Hasta 15 Kg", Tarifa = 40.00m, TarifaDisplay = "S/. 40.00" },
            new PresetItemViewModel { Nombre = "Carga Especial", Icono = "TruckRampBox", Descripcion = "Por Kg adicional", Tarifa = 50.00m, TarifaDisplay = "S/. 50.00" }
        };

        private PresetItemViewModel? _presetSeleccionado;
        public PresetItemViewModel? PresetSeleccionado
        {
            get => _presetSeleccionado;
            set
            {
                if (SetProperty(ref _presetSeleccionado, value))
                {
                    if (value != null)
                    {
                        EncomiendaCosto = value.Tarifa;
                    }
                    CalcularLiquidacion();
                }
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

        private decimal _encomiendaCosto;
        public decimal EncomiendaCosto
        {
            get => _encomiendaCosto;
            set => SetProperty(ref _encomiendaCosto, value);
        }
        public decimal TarifaPorKg => 2.50m;
        public decimal EncomiendaPesoKg { get; set; } = 5.0m;
        public string EncomiendaPesoKgTexto { get; set; } = "5.0";
        public string EncomiendaDescripcion { get; set; } = "Paquete sellado";
        public bool EsCargaPersonalizada => false;
        public bool EsPesoMaximoEncomienda => false;
        public string TerminalLlegadaDisplay => ViajeSeleccionado != null
            ? $"Agencia Central {ViajeSeleccionado.Destino} — Terminal Terrestre"
            : "Agencia Central de Destino — Terminal Terrestre";
        public string DireccionEntrega { get; set; } = "";
        public string ReferenciaEntrega { get; set; } = "";

        // Encomienda Remitente / Destinatario
        public ObservableCollection<string> TiposDocumentoDisponibles { get; } = new() { "DNI", "RUC", "C.E." };

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
        public string RemitenteDoc { get; set; } = "";
        public int RemitenteDocMaxLength => RemitenteTipoDoc == "RUC" ? 11 : 8;
        public string RemitenteNombre { get; set; } = "";
        public string RemitenteTelefono { get; set; } = "";

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
        public string DestinatarioDoc { get; set; } = "";
        public int DestinatarioDocMaxLength => DestinatarioTipoDoc == "RUC" ? 11 : 8;
        public string DestinatarioNombre { get; set; } = "";
        public string DestinatarioTelefono { get; set; } = "";

        // --- Liquidación de Venta ---

        public decimal TotalBoletos => AsientosSeleccionados.Count * (ViajeSeleccionado?.PrecioBase ?? 0m);
        public decimal TotalVenta => TotalBoletos + (EsSoloEncomienda || IncluyeEncomienda ? EncomiendaCosto + RecargoDelivery : 0m);

        public bool PuedeContinuarAPasajeros => AsientosSeleccionados.Count > 0 || EsSoloEncomienda;
        public string TextoBotonContinuar => EsSoloEncomienda ? "Continuar a Guía de Encomienda ➔" : (AsientosSeleccionados.Count > 0 ? $"Continuar con {AsientosSeleccionados.Count} pasajero(s) ➔" : "Seleccione al menos 1 asiento");
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
                    CalcularLiquidacion();
                }
            }
        }

        public bool EsPagoEfectivo => MetodoPagoSeleccionado == "Efectivo";
        public bool EsPagoDigital => MetodoPagoSeleccionado != "Efectivo";

        private decimal _montoRecibido = 100.00m;
        public decimal MontoRecibido
        {
            get => _montoRecibido;
            set
            {
                if (SetProperty(ref _montoRecibido, value))
                {
                    OnPropertyChanged(nameof(Vuelto));
                    OnPropertyChanged(nameof(FaltaDinero));
                    OnPropertyChanged(nameof(DiferenciaFaltante));
                }
            }
        }

        public decimal Vuelto => MontoRecibido >= TotalVenta ? MontoRecibido - TotalVenta : 0m;
        public bool FaltaDinero => EsPagoEfectivo && MontoRecibido < TotalVenta;
        public decimal DiferenciaFaltante => TotalVenta - MontoRecibido;
        public string NroOperacion { get; set; } = "";
        public int MaxLongitudOperacion => 12;

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

            _presetSeleccionado = PresetsDisponibles[0];
            _encomiendaCosto = _presetSeleccionado.Tarifa;

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

                        if (OrigenesDisponibles.Count > 0)
                        {
                            OrigenSeleccionado = OrigenesDisponibles.Contains("Lima") ? "Lima" : OrigenesDisponibles[0];
                        }
                    });
                });

                // 3. Ejecutar búsqueda inicial de viajes
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BuscarViajes();
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
                        PrecioBase = precioBase
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
            OrigenSeleccionado = OrigenesDisponibles.Count > 0 ? (OrigenesDisponibles.Contains("Lima") ? "Lima" : OrigenesDisponibles[0]) : null;
            ViajesDisponibles.Clear();
            ViajeSeleccionado = null;
            MostrarResultadosViajes = false;
            MostrarMapaAsientos = false;
            AsientosSeleccionados.Clear();
            _todosLosAsientos.Clear();
            FilasAsientosVisibles.Clear();
            Pasajeros.Clear();
            MostrarPanelViajes = true;
            MostrarPanelPasajeros = false;
            MostrarPanelPago = false;
            MetodoPagoSeleccionado = "Efectivo";
            MontoRecibido = 0m;
            NroOperacion = "";
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
            ViajeSeleccionado = viaje;
            MostrarMapaAsientos = true;
            AsientosSeleccionados.Clear();

            ConsultarAsientosDesdeBd(viaje.ViajeID);
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

        private void ToggleAsiento(AsientoItemViewModel asiento)
        {
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

            OnPropertyChanged(nameof(TextoBotonContinuar));
            OnPropertyChanged(nameof(PuedeContinuarAPasajeros));
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

        private void ConfirmarVenta()
        {
            if (EsPagoEfectivo && FaltaDinero)
            {
                MessageBox.Show($"El monto recibido es insuficiente para completar la venta. Faltan S/. {DiferenciaFaltante:N2}.", "Pago Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string detallePago = EsPagoEfectivo
                ? $"Método de Pago: Efectivo (Entregado: S/. {MontoRecibido:N2} | Vuelto: S/. {Vuelto:N2})"
                : $"Método de Pago: {MetodoPagoSeleccionado}" + (string.IsNullOrWhiteSpace(NroOperacion) ? "" : $" (Ref: {NroOperacion.Trim()})");

            string mensaje;
            if (EsSoloEncomienda)
            {
                string modalidad = EsEntregaDomicilio ? "Entrega a Domicilio (+S/. 10.00)" : "Recojo en Agencia";
                mensaje = $"¡DESPACHO DE ENCOMIENDA CONFIRMADO CON ÉXITO!\n\nModalidad: {modalidad}\nTotal Pagado: S/. {TotalVenta:N2}\n{detallePago}";
            }
            else
            {
                string asientos = string.Join("\n", Pasajeros.Select(p => $"• Asiento #{p.NroAsiento} (Piso {p.Piso}): {p.Nombres} - DNI: {p.Dni} (S/. {p.Precio:N2})"));
                mensaje = $"¡VENTA CONFIRMADA CON ÉXITO!\n\nBoletos Emitidos:\n{asientos}\n\nTotal Pagado: S/. {TotalVenta:N2}\n{detallePago}";
            }

            MessageBox.Show(mensaje, "Emisión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            LimpiarBusqueda();
        }

        private void AplicarMontoRapido(string? valor)
        {
            if (valor == "Exacto")
            {
                MontoRecibido = TotalVenta;
            }
            else if (valor != null && valor.StartsWith("+") && decimal.TryParse(valor.Substring(1), out decimal suma))
            {
                MontoRecibido += suma;
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
