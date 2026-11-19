using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SchoolOS.BuildingBlocks.Secrets;

public static class SecretsDependencyInjection
{
    public const string UncachedProviderKey = "schoolos-secrets-uncached";

    public static readonly IReadOnlyDictionary<SecretKey, string> CloudStartupSecrets = new Dictionary<SecretKey, string>
    {
        [SecretKey.Platform("platform-db-connection")] = "ConnectionStrings:PlatformDatabase",
        [SecretKey.Platform("jwt-signing-key")] = "Authentication:Jwt:SigningKey",
    };

    public static readonly IReadOnlyDictionary<SecretKey, string> LocalStartupSecrets = new Dictionary<SecretKey, string>
    {
        [SecretKey.Platform("local-db-connection")] = "ConnectionStrings:SchoolOSDatabase",
        [SecretKey.Platform("jwt-signing-key")] = "Authentication:Jwt:SigningKey",
    };

    public static IServiceCollection AddSecretsBuildingBlocks(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SecretsOptions.SectionName);

        services.AddOptions<SecretsOptions>().Bind(section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecretsOptions>, SecretsOptionsValidator>();
        services.AddScoped<ITenantSecretProvider, TenantSecretProvider>();

        if (ReadOptions(configuration).Kind == SecretProviderKind.None)
        {
            services.AddSingleton<ISecretProvider, NotConfiguredSecretProvider>();
            return services;
        }

        services.AddMemoryCache();

        services.AddKeyedSingleton<ISecretProvider>(UncachedProviderKey, (provider, _) =>
            new ResilientSecretProvider(
                SecretProviderFactory.Create(provider.GetRequiredService<IOptions<SecretsOptions>>().Value),
                ResilientSecretProvider.CreatePipeline(provider.GetRequiredService<ILogger<ResilientSecretProvider>>())));

        services.AddMetrics();
        services.TryAddSingleton<SecretsTelemetry>();

        services.AddSingleton<ISecretProvider>(provider => new InstrumentedSecretProvider(
            new CachedSecretProvider(
                provider.GetRequiredKeyedService<ISecretProvider>(UncachedProviderKey),
                provider.GetRequiredService<IMemoryCache>(),
                provider.GetRequiredService<IOptions<SecretsOptions>>(),
                provider.GetRequiredService<SecretsTelemetry>()),
            provider.GetRequiredService<IOptions<SecretsOptions>>().Value.Kind?.ToString() ?? "unknown"));

        services.AddHealthChecks().Add(new HealthCheckRegistration(
            SecretProviderHealthCheck.Name,
            provider => new SecretProviderHealthCheck(provider.GetRequiredKeyedService<ISecretProvider>(UncachedProviderKey)),
            failureStatus: null,
            tags: null));

        return services;
    }

    public static async Task AddPlatformSecretsAsync(
        this IHostApplicationBuilder builder,
        IReadOnlyDictionary<SecretKey, string> startupSecrets,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(startupSecrets);

        var options = ReadOptions(builder.Configuration);
        var errors = SecretsOptionsValidator.GetErrors(options);
        if (errors.Count > 0)
        {
            throw new OptionsValidationException(SecretsOptions.SectionName, typeof(SecretsOptions), errors);
        }

        if (options.Kind == SecretProviderKind.None)
        {
            return;
        }

        var store = SecretProviderFactory.Create(options);
        try
        {
            var provider = new ResilientSecretProvider(store, ResilientSecretProvider.CreatePipeline(NullLogger.Instance));
            var values = new Dictionary<string, string?>();

            foreach (var (key, configurationKey) in startupSecrets)
            {
                if (await provider.GetSecretAsync(key, cancellationToken) is { } value)
                {
                    values[configurationKey] = value;
                }
            }

            if (values.Count > 0)
            {
                builder.Configuration.AddInMemoryCollection(values);
            }
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    private static SecretsOptions ReadOptions(IConfiguration configuration) =>
        configuration.GetSection(SecretsOptions.SectionName).Get<SecretsOptions>() ?? new SecretsOptions();
}
