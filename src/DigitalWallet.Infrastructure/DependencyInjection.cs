using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Infrastructure.Data.Interceptors;
using DigitalWallet.Infrastructure.Services;
using DigitalWallet.Application.Interfaces;


namespace DigitalWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<AuditableEntityInterceptor>();// registering the interceptor into the DI container

        services.AddDbContext<AppDbContext>((sp, options) =>
        options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()),// attaches the interceptor to EF
           optionsLifetime: ServiceLifetime.Singleton);
           // both addDbContext and addDbContextFactory registers DbContextOptions<AppDbContext>.
           // Factory is singleton itself thus we match it for both of them. 
           // It also makes sense to have singleton lifetime for DbContextOptions because it is thread-safe and can be shared across multiple DbContext instances.
           // And because of that AuditableEntityInterceptor has to be singleton as well, 
           // otherwise it will throw an exception about scoped service in singleton.
           

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProcessLogger, ProcessLogger>();

        services.AddSingleton(TimeProvider.System);

        var pepper = configuration["CardSettings:Pepper"]
        ?? throw new InvalidOperationException("CardSettings:Pepper is not configured.");

        services.AddSingleton<ICardGenerator>(sp =>
            new CardGenerator(pepper, sp.GetRequiredService<TimeProvider>()));

        return services;
    }
}