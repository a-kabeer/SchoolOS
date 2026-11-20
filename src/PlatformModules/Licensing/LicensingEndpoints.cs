using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.PlatformModules.Licensing.Application;

namespace SchoolOS.PlatformModules.Licensing.Endpoints;

public sealed record IssueLeaseRequestBody(int? LeaseDays = null);

public sealed class PlatformLicensingEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/licensing";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(GetPublicKey, "public-key").AllowAnonymous();

        var adminGroup = groupBuilder.MapGroup(string.Empty)
            .RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        adminGroup.MapNamedPost(IssueForTenant, "leases/{tenantId:guid}");
    }

    private static IResult GetPublicKey(LicensingSigningOptions options)
        => Results.Ok(new { options.KeyId, Algorithm = "ES256", options.PublicKeyPem });

    private static async Task<IResult> IssueForTenant(
        Guid tenantId,
        IssueLeaseRequestBody? request,
        LicenseLeaseIssuer issuer,
        CancellationToken cancellationToken)
    {
        var lease = await issuer.IssueLeaseAsync(tenantId, request?.LeaseDays, cancellationToken);
        return Results.Ok(lease);
    }
}

public sealed class TenantLicensingEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/licensing";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);
        groupBuilder.MapNamedPost(RefreshOwnLease, "lease/refresh");
    }

    private static async Task<IResult> RefreshOwnLease(
        ITenantContext tenantContext,
        LicenseLeaseIssuer issuer,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Forbid();
        }

        var lease = await issuer.IssueLeaseAsync(tenantContext.TenantId.Value, cancellationToken: cancellationToken);
        return Results.Ok(lease);
    }
}
