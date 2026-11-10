using Microsoft.Extensions.DependencyInjection;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;

namespace SchoolOS.BuildingBlocks.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<PostgresTenantSessionInterceptor>();
        services.AddScoped<PostgresTenantTransactionInterceptor>();
        return services;
    }
}
