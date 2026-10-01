using Microsoft.Extensions.DependencyInjection;
using SistemaTransportes.Application.Interfaces;
using SistemaTransportes.Application.Services;

namespace SistemaTransportes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConsultaViajesService, ConsultaViajesService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        return services;
    }
}
