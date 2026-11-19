using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;

namespace SchoolOS.BuildingBlocks.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceBuildingBlocks(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ChangeAuditInterceptor>();
        services.AddScoped<PostgresTenantSessionInterceptor>();
        services.AddScoped<PostgresTenantTransactionInterceptor>();
        return services;
    }
}
