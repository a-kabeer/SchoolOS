using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SchoolOS.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    private static readonly PathString[] UntracedPaths = ["/health", "/healthz", "/alive", "/metrics"];

    public static IServiceCollection AddObservabilityBuildingBlocks(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        IHostEnvironment? environment = null,
        string? defaultServiceName = null)
    {
        services.AddHealthChecks();

        if (configuration is null || environment is null)
        {
            services.AddOptions<ObservabilityOptions>();
            return services;
        }

        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        var options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (string.IsNullOrWhiteSpace(configuration[$"{ObservabilityOptions.SectionName}:ServiceName"])
            && !string.IsNullOrWhiteSpace(defaultServiceName))
        {
            options.ServiceName = defaultServiceName;
        }

        var errors = options.GetErrors();
        if (errors.Count > 0)
        {
            throw new OptionsValidationException(ObservabilityOptions.SectionName, typeof(ObservabilityOptions), errors);
        }

        services.AddOptions<ObservabilityOptions>().Bind(section);

        if (!options.Enabled)
        {
            return services;
        }

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: ServiceVersion())
                .AddAttributes(
                [
                    new("deployment.environment", environment.EnvironmentName),
                    new("deployment.environment.name", environment.EnvironmentName),
                ]))
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SampleRatioFor(environment))))
                .AddAspNetCoreInstrumentation(aspNetCore => aspNetCore.Filter = IsTraced)
                .AddHttpClientInstrumentation()
                .AddSource(TelemetryNames.Npgsql)
                .AddSource([.. TelemetryNames.ActivitySources]))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(TelemetryNames.Npgsql)
                .AddMeter([.. TelemetryNames.Meters]))
            .WithLogging(configureBuilder: null, configureOptions: logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            builder.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(options.OtlpEndpoint));
        }

        return services;
    }

    public static IApplicationBuilder UseObservabilityBuildingBlocks(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseTenantTelemetry(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TenantTelemetryMiddleware>();
    }

    private static bool IsTraced(HttpContext context) =>
        !UntracedPaths.Any(path => context.Request.Path.StartsWithSegments(path));

    private static string? ServiceVersion() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
