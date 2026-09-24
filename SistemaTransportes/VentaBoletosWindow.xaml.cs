using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaTransportes.Common;

namespace SistemaTransportes
{
    public partial class VentaBoletosWindow : Window
    {
        private static readonly Regex SoloNumerosRegex = new("^[0-9]+$", RegexOptions.Compiled);

        public VentaBoletosWindow()
        {
            InitializeComponent();
            DataContext = new VentaIntegradaViewModel(this);
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
        public int Piso { get; set; }
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

        public UsuarioSessionModel Session { get; } = new();

        private decimal _saldoCajaActual = 500.00m;
        public decimal SaldoCajaActual
        {
            get => _saldoCajaActual;
            set => SetProperty(ref _saldoCajaActual, value);
        }

        // --- Búsqueda y Rutas ---
        public ObservableCollection<string> OrigenesDisponibles { get; } =
            new() { "Lima", "Huancayo", "Arequipa", "Trujillo", "Cusco" };

        public ObservableCollection<string> DestinosDisponibles { get; } =
            new() { "Huancayo", "Lima", "Arequipa", "Ayacucho", "Chiclayo" };

        private string? _origenSeleccionado;
        public string? OrigenSeleccionado
        {
            get => _origenSeleccionado;
            set => SetProperty(ref _origenSeleccionado, value);
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

        public bool EsEntregaAgencia { get; set; } = true;
        public bool EsEntregaDomicilio { get; set; }
        public string ModalidadEntrega => EsEntregaDomicilio ? "Domicilio" : "Agencia";
        public decimal RecargoDelivery => EsEntregaDomicilio ? 10.00m : 0.00m;

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
        public string TerminalLlegadaDisplay => "Terminal Principal";
        public string DireccionEntrega { get; set; } = "";
        public string ReferenciaEntrega { get; set; } = "";

        // Encomienda Remitente / Destinatario
        public ObservableCollection<string> TiposDocumentoDisponibles { get; } = new() { "DNI", "RUC", "C.E." };
        public string RemitenteTipoDoc { get; set; } = "DNI";
        public string RemitenteDoc { get; set; } = "";
        public int RemitenteDocMaxLength => 8;
        public string RemitenteNombre { get; set; } = "";
        public string RemitenteTelefono { get; set; } = "";

        public string DestinatarioTipoDoc { get; set; } = "DNI";
        public string DestinatarioDoc { get; set; } = "";
        public int DestinatarioDocMaxLength => 8;
        public string DestinatarioNombre { get; set; } = "";
        public string DestinatarioTelefono { get; set; } = "";

        // --- Pasajeros y Liquidación ---
        public ObservableCollection<PasajeroItemViewModel> Pasajeros { get; } = new();

        public decimal TotalBoletos => AsientosSeleccionados.Count * (ViajeSeleccionado?.PrecioBase ?? 0m);
        public decimal TotalVenta => TotalBoletos + (EsSoloEncomienda || IncluyeEncomienda ? EncomiendaCosto + RecargoDelivery : 0m);

        public bool PuedeContinuarAPasajeros => AsientosSeleccionados.Count > 0 || EsSoloEncomienda;
        public string TextoBotonContinuar => EsSoloEncomienda ? "Continuar a Guía de Encomienda ➔" : (AsientosSeleccionados.Count > 0 ? $"Continuar con {AsientosSeleccionados.Count} pasajero(s) ➔" : "Seleccione al menos 1 asiento");
        public string TextoBotonContinuarAPago => "Continuar al Paso 3: Pago ➔";
        public string TextoBotonConfirmarPago => "Confirmar Pago y Emitir";
        public string TextoBotonFooterConfirmar => "Confirmar Venta y Emitir Comprobante";
        public string TextoBotonVolverDePaso2 => "Volver a Salidas";
        public string TextoBotonVolverDePago => "Volver a Pasajeros";

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
        public ICommand CerrarSesionCommand { get; }
        public ICommand RefrescarMapaCommand { get; }
        public ICommand MontoRapidoCommand { get; }

        public VentaIntegradaViewModel(Window window)
        {
            _window = window;

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

            InicializarCroquisBase();
        }

        private void BuscarViajes()
        {
            ViajesDisponibles.Clear();
            string orig = string.IsNullOrWhiteSpace(OrigenSeleccionado) ? "Lima" : OrigenSeleccionado;
            string dest = string.IsNullOrWhiteSpace(DestinoSeleccionado) ? "Huancayo" : DestinoSeleccionado;

            ViajesDisponibles.Add(new ViajeItemViewModel
            {
                ViajeID = 1,
                Origen = orig,
                Destino = dest,
                BusPlaca = "ABC-123",
                TipoServicio = "Servicio Directo",
                Categoria = "VIP",
                HoraSalidaTexto = "08:00 AM",
                HoraLlegadaTexto = "03:30 PM",
                Duracion = "07h 30m",
                FechaHoraSalida = DateTime.Today.AddHours(8),
                FechaHoraLlegada = DateTime.Today.AddHours(15).AddMinutes(30),
                PrecioBase = 65.00m
            });

            ViajesDisponibles.Add(new ViajeItemViewModel
            {
                ViajeID = 2,
                Origen = orig,
                Destino = dest,
                BusPlaca = "XYZ-789",
                TipoServicio = "Servicio Ejecutivo",
                Categoria = "Ejecutivo",
                HoraSalidaTexto = "01:30 PM",
                HoraLlegadaTexto = "09:00 PM",
                Duracion = "07h 30m",
                FechaHoraSalida = DateTime.Today.AddHours(13).AddMinutes(30),
                FechaHoraLlegada = DateTime.Today.AddHours(21),
                PrecioBase = 55.00m
            });

            ViajesDisponibles.Add(new ViajeItemViewModel
            {
                ViajeID = 3,
                Origen = orig,
                Destino = dest,
                BusPlaca = "PER-456",
                TipoServicio = "Servicio Premium",
                Categoria = "Premium",
                HoraSalidaTexto = "09:00 PM",
                HoraLlegadaTexto = "04:30 AM",
                Duracion = "07h 30m",
                FechaHoraSalida = DateTime.Today.AddHours(21),
                FechaHoraLlegada = DateTime.Today.AddDays(1).AddHours(4).AddMinutes(30),
                PrecioBase = 80.00m
            });

            MostrarResultadosViajes = true;
        }

        private void LimpiarBusqueda()
        {
            OrigenSeleccionado = null;
            DestinoSeleccionado = null;
            ViajesDisponibles.Clear();
            ViajeSeleccionado = null;
            MostrarResultadosViajes = false;
            MostrarMapaAsientos = false;
            AsientosSeleccionados.Clear();
            FilasAsientosVisibles.Clear();
            MostrarPanelViajes = true;
            MostrarPanelPasajeros = false;
            MostrarPanelPago = false;
            CalcularLiquidacion();
        }

        private void IntercambiarCiudades()
        {
            string? temp = OrigenSeleccionado;
            OrigenSeleccionado = DestinoSeleccionado;
            DestinoSeleccionado = temp;
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
            CargarAsientos();
        }

        private void InicializarCroquisBase()
        {
            _todosLosAsientos.Clear();
            // Piso 1: 20 asientos
            for (int i = 1; i <= 20; i++)
            {
                var a = new AsientoItemViewModel
                {
                    NroAsiento = i,
                    Piso = 1,
                    Estado = (i == 3 || i == 4 || i == 11 || i == 12) ? "Ocupado" : "Libre"
                };
                a.ClickCommand = new RelayCommand(() => ToggleAsiento(a));
                _todosLosAsientos.Add(a);
            }
            // Piso 2: 28 asientos
            for (int i = 21; i <= 48; i++)
            {
                var a = new AsientoItemViewModel
                {
                    NroAsiento = i,
                    Piso = 2,
                    Estado = (i == 23 || i == 24 || i == 35 || i == 36 || i == 42) ? "Ocupado" : "Libre"
                };
                a.ClickCommand = new RelayCommand(() => ToggleAsiento(a));
                _todosLosAsientos.Add(a);
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
            CargarAsientos();
        }

        private void IrAPasajeros()
        {
            Pasajeros.Clear();
            foreach (var a in AsientosSeleccionados)
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
            MostrarPanelViajes = false;
            MostrarPanelPasajeros = false;
            MostrarPanelPago = true;
            CalcularLiquidacion();
        }

        private void VolverAPasajeros()
        {
            MostrarPanelViajes = false;
            MostrarPanelPasajeros = true;
            MostrarPanelPago = false;
        }

        private void ConfirmarVenta()
        {
            MessageBox.Show(
                $"¡Venta registrada con éxito!\n\n" +
                $"• Ruta: {ViajeSeleccionado?.RutaTexto}\n" +
                $"• Total Boletos: S/. {TotalBoletos:N2}\n" +
                $"• Total Liquidado: S/. {TotalVenta:N2}\n" +
                $"• Método de Pago: {MetodoPagoSeleccionado}\n\n" +
                $"Comprobante emitido correctamente.",
                "Emisión Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Marcar asientos como ocupados
            foreach (var a in AsientosSeleccionados)
            {
                a.Estado = "Ocupado";
            }
            AsientosSeleccionados.Clear();

            // Retornar a la vista inicial
            MostrarPanelViajes = true;
            MostrarPanelPasajeros = false;
            MostrarPanelPago = false;
            MostrarMapaAsientos = false;
            CalcularLiquidacion();
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
