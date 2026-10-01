using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.Domain.Repositories;
using SistemaTransportes.Infrastructure.Connection;
using SistemaTransportes.Infrastructure.Repositories;

namespace SistemaTransportes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICajaTurnoRepository, CajaTurnoRepository>();
        services.AddScoped<IViajeRepository, ViajeRepository>();
        services.AddScoped<IAsientoRepository, AsientoRepository>();
        services.AddScoped<IHistorialRepository, HistorialRepository>();

        return services;
    }
}
