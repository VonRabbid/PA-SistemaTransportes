using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.UI.ViewModels;

namespace SistemaTransportes;

/// <summary>
/// Ventana para consultar el historial de ventas del turno en tiempo real.
/// Code-behind desacoplado bajo el patrón MVVM Puro: contiene únicamente inicialización
/// de componentes, asignación de DataContext y eventos de interfaz gráfica.
/// </summary>
public partial class HistorialVentasWindow : Window
{
    public HistorialVentasViewModel ViewModel { get; }

    public HistorialVentasWindow(HistorialVentasViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarHistorialAsync();
        };
    }

    public HistorialVentasWindow(UsuarioSessionModel session)
        : this(App.CurrentServices.GetRequiredService<IAuditoriaServiceScopedFactory>().CrearViewModel(session))
    {
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
