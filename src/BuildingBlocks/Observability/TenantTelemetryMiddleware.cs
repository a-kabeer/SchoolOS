using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Observability;

/// <summary>
/// Attaches server-derived <c>tenant.id</c> (and optional <c>campus.id</c>) from <see cref="ITenantContext"/>
/// onto the active OpenTelemetry span and logging scope. Must run after <c>UseAuthentication()</c>.
/// </summary>
public sealed class TenantTelemetryMiddleware(
    RequestDelegate next,
    ILogger<TenantTelemetryMiddleware> logger,
    IOptions<ObservabilityOptions> options)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (tenantContext.TenantId is not { } tenantIdGuid)
        {
            await next(context);
            return;
        }

        var tenantId = tenantIdGuid.ToString("D");
        var activity = context.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
        activity?.SetTag(TelemetryNames.Tags.TenantId, tenantId);

        var scopeValues = new Dictionary<string, object?>
        {
            [TelemetryNames.Tags.TenantId] = tenantId
        };

        if (tenantContext.CampusId is { } campusIdGuid)
        {
            var campusId = campusIdGuid.ToString("D");
            activity?.SetTag(TelemetryNames.Tags.CampusId, campusId);
            scopeValues[TelemetryNames.Tags.CampusId] = campusId;
        }

        if (options.Value.TenantIdAsMetricLabel)
        {
            context.Features.Get<IHttpMetricsTagsFeature>()?.Tags.Add(new(TelemetryNames.Tags.TenantId, tenantId));
        }

        using (logger.BeginScope(scopeValues))
        {
            await next(context);
        }
    }
}
