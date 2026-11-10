using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SchoolOS.BuildingBlocks.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityBuildingBlocks(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        var jwtOptions = new JwtOptions();
        if (configuration is not null)
        {
            configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
        }
        services.AddSingleton(jwtOptions);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(5)
            };
        });

        services.AddAuthorization(options =>
        {
            // PlatformAdmin policy: required for Cloud Control Plane operations
            options.AddPolicy(SecurityPolicies.PlatformAdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole(SecurityPolicies.PlatformAdminRole) ||
                    ctx.User.HasClaim("role", SecurityPolicies.PlatformAdminRole) ||
                    ctx.User.HasClaim(TenantContext.ScopeClaimType, SecurityPolicies.PlatformAdminScope));
            });

            // TenantScoped policy: required for Local Data Plane tenant operations
            options.AddPolicy(SecurityPolicies.TenantScopedPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                {
                    var tenantClaim = ctx.User.FindFirst(TenantContext.TenantIdClaimType)?.Value;
                    return Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty;
                });
            });
        });

        return services;
    }
}
