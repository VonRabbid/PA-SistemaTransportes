using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SistemaTransportes.Common;

namespace SistemaTransportes
{
    /// <summary>
    /// Ventana para consultar el historial de ventas del turno en tiempo real conectado a Azure SQL.
    /// </summary>
    public partial class HistorialVentasWindow : Window
    {
        public HistorialVentasViewModel ViewModel { get; }

        public HistorialVentasWindow(UsuarioSessionModel session)
        {
            InitializeComponent();
            ViewModel = new HistorialVentasViewModel(session);
            DataContext = ViewModel;

            Loaded += async (s, e) =>
            {
                await ViewModel.CargarHistorialAsync();
            };
        }

        private void TxtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtBusqueda.Text) ? Visibility.Visible : Visibility.Collapsed;
            ViewModel.AplicarFiltro(TxtBusqueda.Text);
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarHistorialAsync();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    #region Modelos de Datos para Auditoría

    public class BoletoHistorialItem
    {
        public int BoletoID { get; set; }
        public string CodigoBoleto => $"BOL-{BoletoID:D5}";
        public DateTime FechaEmision { get; set; }
        public string FechaHoraStr => FechaEmision.ToString("dd/MM/yyyy HH:mm");
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Ruta => $"{Origen} ➔ {Destino}";
        public int NroAsiento { get; set; }
        public string AsientoStr => $"Asiento #{NroAsiento}";
        public string DniPasajero { get; set; } = string.Empty;
        public string NombrePasajero { get; set; } = string.Empty;
        public string PasajeroInfo => $"{DniPasajero} - {NombrePasajero}";
        public decimal PrecioFinal { get; set; }
        public string PrecioStr => $"S/. {PrecioFinal:N2}";
        public string MetodoPago { get; set; } = "Efectivo";
        public string NumeroOperacion { get; set; } = string.Empty;
        public string OperacionStr => string.IsNullOrWhiteSpace(NumeroOperacion) ? "-" : NumeroOperacion;
    }

    public class EncomiendaHistorialItem
    {
        public int EncomiendaID { get; set; }
        public string CodigoEncomienda => $"GUIA-{EncomiendaID:D5}";
        public DateTime FechaRecepcion { get; set; }
        public string FechaHoraStr => FechaRecepcion.ToString("dd/MM/yyyy HH:mm");
        public string RemitenteDoc { get; set; } = string.Empty;
        public string RemitenteNombre { get; set; } = string.Empty;
        public string RemitenteInfo => string.IsNullOrWhiteSpace(RemitenteDoc) ? RemitenteNombre : $"{RemitenteDoc} - {RemitenteNombre}";
        public string DestinatarioDoc { get; set; } = string.Empty;
        public string DestinatarioNombre { get; set; } = string.Empty;
        public string DestinatarioInfo => string.IsNullOrWhiteSpace(DestinatarioDoc) ? DestinatarioNombre : $"{DestinatarioDoc} - {DestinatarioNombre}";
        public string ModalidadEntrega { get; set; } = "Agencia";
        public string Descripcion { get; set; } = string.Empty;
        public decimal PesoKg { get; set; }
        public string PesoStr => $"{PesoKg:N1} kg";
        public decimal TotalCarga { get; set; }
        public string CostoTotalStr => $"S/. {TotalCarga:N2}";
        public string MetodoPago { get; set; } = "Efectivo";
        public string NumeroOperacion { get; set; } = string.Empty;
        public string MetodoPagoInfo => string.IsNullOrWhiteSpace(NumeroOperacion) ? MetodoPago : $"{MetodoPago} (Ref: {NumeroOperacion})";
    }

    #endregion

    #region ViewModel de Historial de Ventas

    public class HistorialVentasViewModel : ViewModelBase
    {
        public UsuarioSessionModel Session { get; }

        private List<BoletoHistorialItem> _todosBoletos = new();
        private List<EncomiendaHistorialItem> _todasEncomiendas = new();

        public ObservableCollection<BoletoHistorialItem> BoletosFiltrados { get; } = new();
        public ObservableCollection<EncomiendaHistorialItem> EncomiendasFiltradas { get; } = new();

        private string _textoFiltro = string.Empty;
        public string TextoFiltro
        {
            get => _textoFiltro;
            set
            {
                if (SetProperty(ref _textoFiltro, value))
                {
                    AplicarFiltro(value);
                }
            }
        }

        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => SetProperty(ref _estaCargando, value);
        }

        private string _operadorNombre = string.Empty;
        public string OperadorNombre
        {
            get => _operadorNombre;
            set => SetProperty(ref _operadorNombre, value);
        }

        private string _turnoTexto = string.Empty;
        public string TurnoTexto
        {
            get => _turnoTexto;
            set => SetProperty(ref _turnoTexto, value);
        }

        private decimal _saldoCajaActual;
        public decimal SaldoCajaActual
        {
            get => _saldoCajaActual;
            set
            {
                if (SetProperty(ref _saldoCajaActual, value))
                {
                    OnPropertyChanged(nameof(SaldoCajaTexto));
                }
            }
        }

        public string SaldoCajaTexto => $"S/. {SaldoCajaActual:N2}";

        private int _totalBoletosEmitidos;
        public int TotalBoletosEmitidos
        {
            get => _totalBoletosEmitidos;
            set => SetProperty(ref _totalBoletosEmitidos, value);
        }

        private int _totalEncomiendasEmitidas;
        public int TotalEncomiendasEmitidas
        {
            get => _totalEncomiendasEmitidas;
            set => SetProperty(ref _totalEncomiendasEmitidas, value);
        }

        private decimal _totalRecaudado;
        public decimal TotalRecaudado
        {
            get => _totalRecaudado;
            set
            {
                if (SetProperty(ref _totalRecaudado, value))
                {
                    OnPropertyChanged(nameof(TotalRecaudadoTexto));
                }
            }
        }

        public string TotalRecaudadoTexto => $"S/. {TotalRecaudado:N2}";

        public bool HayBoletos => BoletosFiltrados.Count > 0;
        public bool HayEncomiendas => EncomiendasFiltradas.Count > 0;

        public HistorialVentasViewModel(UsuarioSessionModel session)
        {
            Session = session ?? new UsuarioSessionModel();
            OperadorNombre = string.IsNullOrWhiteSpace(Session.Nombres) ? Session.Username : $"{Session.Nombres} ({Session.Username})";
            TurnoTexto = Session.CajaTurnoID.ToString();
        }

        private static string ObtenerCadenaConexion()
        {
            return ConfigurationManager.ConnectionStrings["BD_Transportes"]?.ConnectionString
                ?? "Server=tcp:sistema-transportes-2026.database.windows.net,1433;Initial Catalog=BD_Transportes;Persist Security Info=False;User ID=admin_st;Password=1425PA31%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        public async Task CargarHistorialAsync()
        {
            EstaCargando = true;
            try
            {
                string cs = ObtenerCadenaConexion();
                var boletos = new List<BoletoHistorialItem>();
                var encomiendas = new List<EncomiendaHistorialItem>();
                decimal saldoActual = 0m;

                await Task.Run(async () =>
                {
                    using var conn = new SqlConnection(cs);
                    await conn.OpenAsync();

                    // 1. Consultar Caja
                    const string sqlCaja = @"
                        SELECT MontoActual 
                        FROM dbo.CajasTurno 
                        WHERE CajaTurnoID = @CajaTurnoID;";
                    using (var cmdCaja = new SqlCommand(sqlCaja, conn))
                    {
                        cmdCaja.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = Session.CajaTurnoID;
                        var resultCaja = await cmdCaja.ExecuteScalarAsync();
                        if (resultCaja != null && resultCaja != DBNull.Value)
                        {
                            saldoActual = Convert.ToDecimal(resultCaja);
                        }
                    }

                    // 2. Consultar Boletos del turno activo
                    const string sqlBoletos = @"
                        SELECT b.BoletoID, b.FechaEmision, v.Origen, v.Destino, b.NroAsiento, 
                               b.DniPasajero, b.NombrePasajero, b.PrecioFinal, b.MetodoPago, b.NumeroOperacion
                        FROM dbo.Boletos b
                        INNER JOIN dbo.Viajes v ON b.ViajeID = v.ViajeID
                        WHERE b.CajaTurnoID = @CajaTurnoID
                        ORDER BY b.BoletoID DESC;";
                    using (var cmdBoleto = new SqlCommand(sqlBoletos, conn))
                    {
                        cmdBoleto.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = Session.CajaTurnoID;
                        using var readerB = await cmdBoleto.ExecuteReaderAsync();
                        while (await readerB.ReadAsync())
                        {
                            boletos.Add(new BoletoHistorialItem
                            {
                                BoletoID = readerB.GetInt32(0),
                                FechaEmision = readerB.GetDateTime(1),
                                Origen = readerB.IsDBNull(2) ? "" : readerB.GetString(2),
                                Destino = readerB.IsDBNull(3) ? "" : readerB.GetString(3),
                                NroAsiento = readerB.GetInt32(4),
                                DniPasajero = readerB.IsDBNull(5) ? "" : readerB.GetString(5),
                                NombrePasajero = readerB.IsDBNull(6) ? "" : readerB.GetString(6),
                                PrecioFinal = readerB.IsDBNull(7) ? 0m : readerB.GetDecimal(7),
                                MetodoPago = readerB.IsDBNull(8) ? "Efectivo" : readerB.GetString(8),
                                NumeroOperacion = readerB.IsDBNull(9) ? "" : readerB.GetString(9)
                            });
                        }
                    }

                    // 3. Consultar Encomiendas del turno activo
                    const string sqlEncomiendas = @"
                        SELECT e.EncomiendaID, e.FechaRecepcion, e.RemitenteDoc, e.RemitenteNombre, 
                               e.DestinatarioDoc, e.DestinatarioNombre, e.ModalidadEntrega, e.Descripcion, 
                               e.PesoKg, (e.CostoCarga + ISNULL(e.RecargoDelivery, 0)) AS TotalCarga, 
                               e.MetodoPago, e.NumeroOperacion
                        FROM dbo.Encomiendas e
                        WHERE e.CajaTurnoID = @CajaTurnoID
                        ORDER BY e.EncomiendaID DESC;";
                    using (var cmdEnc = new SqlCommand(sqlEncomiendas, conn))
                    {
                        cmdEnc.Parameters.Add("@CajaTurnoID", SqlDbType.Int).Value = Session.CajaTurnoID;
                        using var readerE = await cmdEnc.ExecuteReaderAsync();
                        while (await readerE.ReadAsync())
                        {
                            encomiendas.Add(new EncomiendaHistorialItem
                            {
                                EncomiendaID = readerE.GetInt32(0),
                                FechaRecepcion = readerE.GetDateTime(1),
                                RemitenteDoc = readerE.IsDBNull(2) ? "" : readerE.GetString(2),
                                RemitenteNombre = readerE.IsDBNull(3) ? "" : readerE.GetString(3),
                                DestinatarioDoc = readerE.IsDBNull(4) ? "" : readerE.GetString(4),
                                DestinatarioNombre = readerE.IsDBNull(5) ? "" : readerE.GetString(5),
                                ModalidadEntrega = readerE.IsDBNull(6) ? "Agencia" : readerE.GetString(6),
                                Descripcion = readerE.IsDBNull(7) ? "" : readerE.GetString(7),
                                PesoKg = readerE.IsDBNull(8) ? 0m : readerE.GetDecimal(8),
                                TotalCarga = readerE.IsDBNull(9) ? 0m : readerE.GetDecimal(9),
                                MetodoPago = readerE.IsDBNull(10) ? "Efectivo" : readerE.GetString(10),
                                NumeroOperacion = readerE.IsDBNull(11) ? "" : readerE.GetString(11)
                            });
                        }
                    }
                });

                _todosBoletos = boletos;
                _todasEncomiendas = encomiendas;
                SaldoCajaActual = saldoActual;

                TotalBoletosEmitidos = _todosBoletos.Count;
                TotalEncomiendasEmitidas = _todasEncomiendas.Count;
                TotalRecaudado = _todosBoletos.Sum(b => b.PrecioFinal) + _todasEncomiendas.Sum(e => e.TotalCarga);

                AplicarFiltro(_textoFiltro);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el historial de ventas: {ex.Message}", "Historial de Ventas", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        public void AplicarFiltro(string? texto)
        {
            _textoFiltro = texto ?? string.Empty;
            string term = _textoFiltro.Trim().ToLowerInvariant();

            BoletosFiltrados.Clear();
            var boletosQuery = string.IsNullOrWhiteSpace(term)
                ? _todosBoletos
                : _todosBoletos.Where(b =>
                    b.BoletoID.ToString().Contains(term) ||
                    b.CodigoBoleto.ToLowerInvariant().Contains(term) ||
                    b.DniPasajero.ToLowerInvariant().Contains(term) ||
                    b.NombrePasajero.ToLowerInvariant().Contains(term) ||
                    b.Origen.ToLowerInvariant().Contains(term) ||
                    b.Destino.ToLowerInvariant().Contains(term) ||
                    b.NroAsiento.ToString().Contains(term) ||
                    b.MetodoPago.ToLowerInvariant().Contains(term) ||
                    b.NumeroOperacion.ToLowerInvariant().Contains(term)
                );

            foreach (var b in boletosQuery)
            {
                BoletosFiltrados.Add(b);
            }

            EncomiendasFiltradas.Clear();
            var encomiendasQuery = string.IsNullOrWhiteSpace(term)
                ? _todasEncomiendas
                : _todasEncomiendas.Where(e =>
                    e.EncomiendaID.ToString().Contains(term) ||
                    e.CodigoEncomienda.ToLowerInvariant().Contains(term) ||
                    e.RemitenteDoc.ToLowerInvariant().Contains(term) ||
                    e.RemitenteNombre.ToLowerInvariant().Contains(term) ||
                    e.DestinatarioDoc.ToLowerInvariant().Contains(term) ||
                    e.DestinatarioNombre.ToLowerInvariant().Contains(term) ||
                    e.ModalidadEntrega.ToLowerInvariant().Contains(term) ||
                    e.Descripcion.ToLowerInvariant().Contains(term) ||
                    e.MetodoPago.ToLowerInvariant().Contains(term) ||
                    e.NumeroOperacion.ToLowerInvariant().Contains(term)
                );

            foreach (var e in encomiendasQuery)
            {
                EncomiendasFiltradas.Add(e);
            }

            OnPropertyChanged(nameof(HayBoletos));
            OnPropertyChanged(nameof(HayEncomiendas));
        }
    }

    #endregion
}
