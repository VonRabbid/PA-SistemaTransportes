using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.UI.ViewModels;

namespace SistemaTransportes;

/// <summary>
/// Lógica de interacción para MainWindow.xaml (Login del Sistema).
/// Vista desacoplada con MVVM puro, sin consultas directas a base de datos ni lógica de negocio.
/// </summary>
public partial class MainWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(LoginViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _serviceProvider = serviceProvider ?? App.CurrentServices;
        DataContext = _viewModel;

        _viewModel.LoginExitoso += OnLoginExitoso;
        _viewModel.NotificarMensaje += (msg, title, isSuccess) =>
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, isSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        };
    }

    public MainWindow() : this(
        App.CurrentServices.GetRequiredService<LoginViewModel>(),
        App.CurrentServices)
    {
    }

    private void OnLoginExitoso(UsuarioSessionModel session)
    {
        var ventanaVenta = _serviceProvider.GetRequiredService<VentaBoletosWindow>();
        ventanaVenta.ViewModel.InicializarSesion(session);
        System.Windows.Application.Current.MainWindow = ventanaVenta;
        ventanaVenta.Show();
        this.Close();
    }
}