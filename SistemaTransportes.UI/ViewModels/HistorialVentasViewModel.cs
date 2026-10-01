using System.Collections.ObjectModel;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Domain.Entities;
using SistemaTransportes.UI.MVVM;

namespace SistemaTransportes.UI.ViewModels;

public class HistorialVentasViewModel : ViewModelBase
{
    private readonly IAuditoriaService _auditoriaService;
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

    public HistorialVentasViewModel(IAuditoriaService auditoriaService, UsuarioSessionModel session)
    {
        _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
        Session = session ?? new UsuarioSessionModel();
        OperadorNombre = string.IsNullOrWhiteSpace(Session.Nombres) ? Session.Username : $"{Session.Nombres} ({Session.Username})";
        TurnoTexto = Session.CajaTurnoID.ToString();
    }

    public async Task CargarHistorialAsync()
    {
        EstaCargando = true;
        try
        {
            var resumen = await _auditoriaService.ObtenerResumenTurnoAsync(Session.CajaTurnoID);
            var boletosDomain = await _auditoriaService.ObtenerBoletosTurnoAsync(Session.CajaTurnoID);
            var encomiendasDomain = await _auditoriaService.ObtenerEncomiendasTurnoAsync(Session.CajaTurnoID);

            var boletos = boletosDomain.Select(b => new BoletoHistorialItem
            {
                BoletoID = b.BoletoID,
                FechaEmision = b.FechaEmision,
                NroAsiento = b.NroAsiento,
                DniPasajero = b.DniPasajero,
                NombrePasajero = b.NombrePasajero,
                PrecioFinal = b.PrecioFinal,
                MetodoPago = b.MetodoPago ?? "Efectivo",
                NumeroOperacion = b.NumeroOperacion ?? string.Empty
            }).ToList();

            var encomiendas = encomiendasDomain.Select(e => new EncomiendaHistorialItem
            {
                EncomiendaID = e.EncomiendaID,
                FechaRecepcion = e.FechaRecepcion,
                RemitenteDoc = e.RemitenteDoc ?? string.Empty,
                RemitenteNombre = e.RemitenteNombre ?? string.Empty,
                DestinatarioDoc = e.DestinatarioDoc ?? string.Empty,
                DestinatarioNombre = e.DestinatarioNombre ?? string.Empty,
                ModalidadEntrega = e.ModalidadEntrega ?? "Agencia",
                Descripcion = e.Descripcion,
                PesoKg = e.PesoKg,
                TotalCarga = e.CostoCarga + (e.RecargoDelivery ?? 0m),
                MetodoPago = e.MetodoPago ?? "Efectivo",
                NumeroOperacion = e.NumeroOperacion ?? string.Empty
            }).ToList();

            _todosBoletos = boletos;
            _todasEncomiendas = encomiendas;

            SaldoCajaActual = resumen.SaldoActual;
            TotalBoletosEmitidos = resumen.TotalBoletos;
            TotalEncomiendasEmitidas = resumen.TotalEncomiendas;
            TotalRecaudado = resumen.TotalRecaudado;

            AplicarFiltro(_textoFiltro);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar historial: {ex.Message}");
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
