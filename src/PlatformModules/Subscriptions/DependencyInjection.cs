using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Subscriptions.Application;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using SchoolOS.PlatformModules.Tenants.Domain;

namespace SchoolOS.PlatformModules.Subscriptions;

public static class DependencyInjection
{
    public static IServiceCollection AddSubscriptionsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableBackgroundWorker = false)
    {
        var connectionString = configuration.GetConnectionString("PlatformDatabase")
            ?? configuration["Database:ConnectionString"];

        services.AddDbContext<SubscriptionsDbContext>((sp, options) =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "subscriptions");
                });
                options.UseSnakeCaseNamingConvention();
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<ChangeAuditInterceptor>(),
                sp.GetRequiredService<PostgresTenantSessionInterceptor>(),
                sp.GetRequiredService<PostgresTenantTransactionInterceptor>());
        });

        services.TryAddSingleton<IPaymentProvider, ManualPaymentProvider>();
        services.TryAddSingleton<IInvoiceNumberGenerator, SequenceInvoiceNumberGenerator>();
        services.AddScoped<SubscriptionManager>();
        services.AddScoped<ITenantTrialProvisioner, SubscriptionTrialProvisioner>();
        services.AddScoped<SubscriptionLifecycleService>();
        services.AddScoped<IModuleAccess, SubscriptionModuleAccess>();

        if (enableBackgroundWorker)
        {
            services.AddHostedService<SubscriptionLifecycleBackgroundService>();
        }

        return services;
    }
}
