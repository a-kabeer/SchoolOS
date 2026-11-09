using Microsoft.Extensions.DependencyInjection;

namespace SchoolOS.BuildingBlocks.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityBuildingBlocks(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        return services;
    }
}
