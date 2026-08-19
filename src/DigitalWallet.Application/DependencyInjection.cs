using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Services;

namespace DigitalWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ITransferService, TransferService>();

        return services;
    }
}