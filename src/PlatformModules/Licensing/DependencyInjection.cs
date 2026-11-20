using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolOS.PlatformModules.Licensing.Application;

namespace SchoolOS.PlatformModules.Licensing;

public static class DependencyInjection
{
    public static IServiceCollection AddLicensingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopmentOrTesting = false)
    {
        var options = new LicensingSigningOptions();
        configuration.GetSection("Licensing").Bind(options);
        options.EnsureKeysConfigured(allowEphemeralGeneration: isDevelopmentOrTesting);

        services.AddSingleton(options);
        services.AddScoped<LicenseLeaseIssuer>();
        return services;
    }
}
