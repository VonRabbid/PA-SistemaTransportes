using System.Configuration;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.Application;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Infrastructure;
using SistemaTransportes.UI.ViewModels;

namespace SistemaTransportes;

/// <summary>
/// Composition Root de la aplicación WPF.
/// Configura el contenedor de Inyección de Dependencias (Microsoft.Extensions.DependencyInjection),
/// registrando servicios de Dominio, Aplicación, Infraestructura y Capa de Presentación (MVVM).
/// </summary>
public partial class App : System.Windows.Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public static IServiceProvider CurrentServices =>
        ((App)Current).ServiceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string connectionString = ConfigurationManager.ConnectionStrings["BD_Transportes"]?.ConnectionString
            ?? "Server=tcp:sistema-transportes-2026.database.windows.net,1433;Initial Catalog=BD_Transportes;Persist Security Info=False;User ID=admin_st;Password=1425PA31%;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        var services = new ServiceCollection();

        // 1. Registro de Capa de Infraestructura
        services.AddInfrastructureServices(connectionString);

        // 2. Registro de Capa de Aplicación
        services.AddApplicationServices();

        // 3. Registro de ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<VentaIntegradaViewModel>();
        services.AddSingleton<IAuditoriaServiceScopedFactory, AuditoriaServiceScopedFactory>();

        // 4. Registro de Vistas (Windows)
        services.AddTransient<MainWindow>();
        services.AddTransient<VentaBoletosWindow>();

        ServiceProvider = services.BuildServiceProvider();

        // 5. Resolución de MainWindow desde el contenedor IoC
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}

internal class AuditoriaServiceScopedFactory : IAuditoriaServiceScopedFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AuditoriaServiceScopedFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public HistorialVentasViewModel CrearViewModel(UsuarioSessionModel session)
    {
        var auditoriaService = _serviceProvider.GetRequiredService<IAuditoriaService>();
        return new HistorialVentasViewModel(auditoriaService, session);
    }
}
