using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Infrastructure.Data.Interceptors;


namespace DigitalWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddScoped<AuditableEntityInterceptor>();// registering the interceptor into the DI container

        services.AddDbContext<AppDbContext>((sp, options) =>
        options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));// attaches the interceptor to EF

        return services;
    }
}