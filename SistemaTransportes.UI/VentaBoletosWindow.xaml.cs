using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.UI.ViewModels;

namespace SistemaTransportes;

/// <summary>
/// Ventana principal de emisión de boletos y encomiendas.
/// Code-behind desacoplado bajo el patrón MVVM Puro: contiene únicamente inicialización
/// de componentes, asignación de DataContext y eventos estrictos de interfaz gráfica.
/// </summary>
public partial class VentaBoletosWindow : Window
{
    private static readonly Regex SoloNumerosRegex = new("^[0-9]+$", RegexOptions.Compiled);
    private readonly VentaIntegradaViewModel _viewModel;

    public VentaIntegradaViewModel ViewModel => _viewModel;

    public VentaBoletosWindow(VentaIntegradaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        ConfigurarEventosViewModel();
    }

    private void ConfigurarEventosViewModel()
    {
        _viewModel.SolicitarCerrarSesion += () =>
        {
            var login = App.CurrentServices.GetRequiredService<MainWindow>();

            System.Windows.Application.Current.MainWindow = login;
            login.Show();
            this.Close();
        };

        _viewModel.SolicitarAbrirHistorial += (session) =>
        {
            var historialFactory = App.CurrentServices.GetRequiredService<IAuditoriaServiceScopedFactory>();
            var historial = new HistorialVentasWindow(historialFactory.CrearViewModel(session));

            historial.Owner = this;
            historial.ShowDialog();
            _ = _viewModel.CargarDatosInicialesAsync();
        };

        _viewModel.NotificarMensaje += (mensaje, titulo, icono) =>
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, icono);
        };
    }

    private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CerrarSesionCommand.Execute(null);
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

public interface IAuditoriaServiceScopedFactory
{
    HistorialVentasViewModel CrearViewModel(UsuarioSessionModel session);
}
