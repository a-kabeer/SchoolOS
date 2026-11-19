using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SchoolOS.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservabilityBuildingBlocks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IApplicationBuilder UseObservabilityBuildingBlocks(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
        return app;
    }
}
