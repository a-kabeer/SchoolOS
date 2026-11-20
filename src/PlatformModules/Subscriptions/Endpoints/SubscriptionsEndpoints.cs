using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.PlatformModules.Subscriptions.Application;
using SchoolOS.PlatformModules.Subscriptions.Domain;
using SchoolOS.PlatformModules.Subscriptions.Persistence;

namespace SchoolOS.PlatformModules.Subscriptions.Endpoints;

public sealed record CreatePlanBody(string Code, string Name, string Description, IReadOnlyList<ModuleCode> PaidModules);
public sealed record CreatePriceBookBody(
    string CountryCode,
    string CurrencyCode,
    decimal TaxRate,
    IReadOnlyList<PriceBookPlanPriceBody> PlanPrices,
    IReadOnlyList<PriceBookModulePriceBody> ModulePrices);

public sealed record PriceBookPlanPriceBody(string PlanCode, BillingCycle BillingCycle, decimal Price);
public sealed record PriceBookModulePriceBody(ModuleCode ModuleCode, BillingCycle BillingCycle, decimal Price);

public sealed record AssignSubscriptionBody(
    Guid TenantId,
    string CountryCode,
    string? PlanCode,
    IReadOnlyList<ModuleCode> AddOnCodes,
    BillingCycle BillingCycle,
    bool IsTrial = false,
    decimal? TaxRateOverride = null);

public sealed record ReconfigureSubscriptionBody(string? PlanCode, IReadOnlyList<ModuleCode> AddOnCodes);
public sealed record CreateInvoiceBody(Guid TenantId, bool NextPeriod);
public sealed record RecordPaymentBody(decimal Amount, PaymentMethod Method, string Reference, DateTimeOffset ReceivedAtUtc);
public sealed record SubmitAddOnRequestBody(ModuleCode ModuleCode, string? Notes);

public sealed class PlatformModulesCatalogEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/modules";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListModules);
        groupBuilder.MapNamedPost(SeedCatalog, "seed");
    }

    private static async Task<IResult> ListModules(SubscriptionsDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.PortalModules.AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.Code, m.Name, m.Description, m.IsCore, m.IsActive, m.SortOrder })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> SeedCatalog(SubscriptionsDbContext db, CancellationToken cancellationToken)
    {
        await SubscriptionCatalogSeeder.SeedAsync(db, cancellationToken);
        return Results.Ok(new { seeded = true });
    }
}

public sealed class PlatformPlansEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/plans";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListPlans);
        groupBuilder.MapNamedPost(CreatePlan);
    }

    private static async Task<IResult> ListPlans(SubscriptionsDbContext db, CancellationToken cancellationToken)
    {
        var plans = await db.Plans.AsNoTracking()
            .Include(p => p.Modules)
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.IsActive,
                ModuleCount = p.Modules.Count
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(plans);
    }

    private static async Task<IResult> CreatePlan(
        CreatePlanBody request,
        SubscriptionsDbContext db,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Plans.AnyAsync(p => p.Code == code, cancellationToken))
        {
            return Results.Conflict(new { code = "PLAN_CODE_TAKEN" });
        }

        var plan = Plan.Create(code, request.Name, request.Description);
        var mods = await db.PortalModules.Where(m => request.PaidModules.Contains(m.Code)).ToListAsync(cancellationToken);
        foreach (var mod in mods)
        {
            plan.AddModule(mod);
        }

        db.Plans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/platform/plans/{plan.Code}", new { plan.Id, plan.Code, plan.Name });
    }
}

public sealed class PlatformPriceBooksEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/price-books";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListPriceBooks);
        groupBuilder.MapNamedPost(CreatePriceBook);
    }

    private static async Task<IResult> ListPriceBooks(SubscriptionsDbContext db, CancellationToken cancellationToken)
    {
        var books = await db.PriceBooks.AsNoTracking()
            .Include(b => b.Items)
            .OrderBy(b => b.CountryCode)
            .Select(b => new
            {
                b.Id,
                b.CountryCode,
                b.CurrencyCode,
                b.TaxRate,
                b.IsActive,
                ItemCount = b.Items.Count
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(books);
    }

    private static async Task<IResult> CreatePriceBook(
        CreatePriceBookBody request,
        SubscriptionsDbContext db,
        CancellationToken cancellationToken)
    {
        await SubscriptionCatalogSeeder.SeedAsync(db, cancellationToken);

        var countryCode = request.CountryCode.Trim().ToUpperInvariant();
        var previous = await db.PriceBooks.Where(b => b.CountryCode == countryCode && b.IsActive).ToListAsync(cancellationToken);
        foreach (var old in previous)
        {
            old.Deactivate();
        }

        var book = PriceBook.Create(countryCode, request.CurrencyCode, request.TaxRate);
        var plans = await db.Plans.AsNoTracking().ToDictionaryAsync(p => p.Code, cancellationToken);
        var modules = await db.PortalModules.AsNoTracking().ToDictionaryAsync(m => m.Code, cancellationToken);

        foreach (var p in request.PlanPrices)
        {
            if (plans.TryGetValue(p.PlanCode.Trim().ToUpperInvariant(), out var plan))
            {
                book.SetPlanPrice(plan.Id, p.BillingCycle, p.Price);
            }
        }

        foreach (var m in request.ModulePrices)
        {
            if (modules.TryGetValue(m.ModuleCode, out var module))
            {
                book.SetModulePrice(module.Id, m.BillingCycle, m.Price);
            }
        }

        db.PriceBooks.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/platform/price-books/{book.Id}", new { book.Id, book.CountryCode, book.CurrencyCode, book.TaxRate });
    }
}

public sealed class PlatformSubscriptionsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/subscriptions";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListSubscriptions);
        groupBuilder.MapNamedPost(AssignSubscription, "assign");
        groupBuilder.MapNamedPut(ReconfigureSubscription, "{tenantId:guid}");
        groupBuilder.MapNamedPost(CancelSubscription, "{tenantId:guid}/cancel");
        groupBuilder.MapNamedPost(RunLifecycleSweep, "lifecycle/sweep");
    }

    private static async Task<IResult> ListSubscriptions(SubscriptionsDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.Subscriptions.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Cloud Control Plane PlatformAdmin cross-tenant query backed by schoolos_platform_admin RLS role
            .IgnoreQueryFilters()
            .Include(s => s.Modules)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new
            {
                s.Id,
                s.TenantId,
                s.CountryCode,
                s.BillingCycle,
                s.CurrencyCode,
                s.Amount,
                s.Status,
                s.StartDate,
                s.EndDate,
                Modules = s.Modules.Where(m => m.Status == TenantModuleStatus.Active).Select(m => m.ModuleCode).ToList()
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> AssignSubscription(
        AssignSubscriptionBody request,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        var sub = await manager.AssignSubscriptionAsync(
            request.TenantId,
            request.CountryCode,
            request.PlanCode,
            request.AddOnCodes,
            request.BillingCycle,
            request.IsTrial,
            request.TaxRateOverride,
            cancellationToken);

        return Results.Created(
            $"/api/v1/platform/subscriptions/{sub.TenantId}",
            new
            {
                sub.Id,
                sub.TenantId,
                sub.Status,
                sub.CurrencyCode,
                sub.Amount,
                sub.EndDate,
                Modules = sub.Modules.Select(m => m.ModuleCode).ToList()
            });
    }

    private static async Task<IResult> ReconfigureSubscription(
        Guid tenantId,
        ReconfigureSubscriptionBody request,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        var sub = await manager.ReconfigureSubscriptionAsync(tenantId, request.PlanCode, request.AddOnCodes, cancellationToken);
        return Results.Ok(new
        {
            sub.Id,
            sub.TenantId,
            sub.Amount,
            Modules = sub.Modules.Where(m => m.Status == TenantModuleStatus.Active).Select(m => m.ModuleCode).ToList()
        });
    }

    private static async Task<IResult> CancelSubscription(
        Guid tenantId,
        SubscriptionsDbContext db,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        using var _ = tenantContext is TenantContext mutable && mutable.IsPlatformAdmin
            ? mutable.BeginPlatformTenantScope(tenantId)
            : throw new UnauthorizedAccessException();

        var sub = await db.Subscriptions
            .Include(s => s.Modules)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken);
        if (sub is null)
        {
            return Results.NotFound();
        }

        sub.Cancel(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { sub.Id, sub.Status });
    }

    private static async Task<IResult> RunLifecycleSweep(
        SubscriptionLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        var result = await lifecycle.RunSweepAsync(cancellationToken);
        return Results.Ok(result);
    }
}

public sealed class PlatformInvoicesEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/invoices";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListInvoices);
        groupBuilder.MapNamedPost(CreateInvoice);
        groupBuilder.MapNamedPost(RecordPayment, "{invoiceId:guid}/payments");
        groupBuilder.MapNamedPost(VoidInvoice, "{invoiceId:guid}/void");
    }

    private static async Task<IResult> ListInvoices(SubscriptionsDbContext db, Guid? tenantId, CancellationToken cancellationToken)
    {
        // compliance-exception: SEC-AUTHZ-03 Cloud Control Plane PlatformAdmin cross-tenant query backed by schoolos_platform_admin RLS role
        var query = db.Invoices.AsNoTracking().IgnoreQueryFilters();
        if (tenantId.HasValue)
        {
            query = query.Where(i => i.TenantId == tenantId.Value);
        }

        var items = await query
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new
            {
                i.Id,
                i.TenantId,
                i.InvoiceNumber,
                i.CurrencyCode,
                i.Subtotal,
                i.TaxRate,
                i.TaxAmount,
                i.Total,
                i.Status,
                i.PeriodStart,
                i.PeriodEnd
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateInvoice(
        CreateInvoiceBody request,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        var invoice = await manager.CreateInvoiceAsync(request.TenantId, request.NextPeriod, cancellationToken);
        return Results.Created(
            $"/api/v1/platform/invoices/{invoice.Id}",
            new
            {
                invoice.Id,
                invoice.TenantId,
                invoice.InvoiceNumber,
                invoice.Subtotal,
                invoice.TaxRate,
                invoice.TaxAmount,
                invoice.Total,
                invoice.Status,
                invoice.PeriodStart,
                invoice.PeriodEnd
            });
    }

    private static async Task<IResult> RecordPayment(
        Guid invoiceId,
        RecordPaymentBody request,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        var payment = await manager.RecordPaymentAsync(
            invoiceId,
            request.Amount,
            request.Method,
            request.Reference,
            request.ReceivedAtUtc,
            cancellationToken);

        return Results.Ok(new { payment.Id, payment.InvoiceId, payment.Amount, payment.Method, payment.Reference });
    }

    private static async Task<IResult> VoidInvoice(
        Guid invoiceId,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        await manager.VoidInvoiceAsync(invoiceId, cancellationToken);
        return Results.NoContent();
    }
}

public sealed class PlatformAddOnRequestsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/platform/addon-requests";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.PlatformAdminPolicy);
        groupBuilder.MapNamedGet(ListAddOnRequests);
        groupBuilder.MapNamedPost(ApproveAddOnRequest, "{id:guid}/approve");
        groupBuilder.MapNamedPost(RejectAddOnRequest, "{id:guid}/reject");
    }

    private static async Task<IResult> ListAddOnRequests(SubscriptionsDbContext db, AddOnRequestStatus? status, CancellationToken cancellationToken)
    {
        // compliance-exception: SEC-AUTHZ-03 Cloud Control Plane PlatformAdmin cross-tenant query backed by schoolos_platform_admin RLS role
        var query = db.AddOnRequests.AsNoTracking().IgnoreQueryFilters();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new { r.Id, r.TenantId, r.ModuleCode, r.Status, r.RequestedAtUtc, r.DecidedAtUtc })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> ApproveAddOnRequest(Guid id, SubscriptionManager manager, CancellationToken cancellationToken)
    {
        var updated = await manager.ApproveAddOnRequestAsync(id, cancellationToken);
        return Results.Ok(new { updated.Id, updated.Status, updated.DecidedAtUtc });
    }

    private static async Task<IResult> RejectAddOnRequest(Guid id, SubscriptionManager manager, CancellationToken cancellationToken)
    {
        var updated = await manager.RejectAddOnRequestAsync(id, cancellationToken);
        return Results.Ok(new { updated.Id, updated.Status, updated.DecidedAtUtc });
    }
}

public sealed class TenantSubscriptionsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/subscriptions";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization(SecurityPolicies.OrganizationAdminPolicy);
        groupBuilder.MapNamedGet(GetCurrent, "current");
        groupBuilder.MapNamedPost(SubmitAddOnRequest, "addon-requests");
    }

    private static async Task<IResult> GetCurrent(
        SubscriptionsDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Forbid();
        }

        var sub = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Modules)
            .FirstOrDefaultAsync(s => s.Status != SubscriptionStatus.Cancelled, cancellationToken);

        if (sub is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            sub.Id,
            sub.Status,
            sub.BillingCycle,
            sub.CurrencyCode,
            sub.Amount,
            sub.StartDate,
            sub.EndDate,
            EnabledModules = sub.Modules
                .Where(m => m.Status == TenantModuleStatus.Active)
                .Select(m => m.ModuleCode)
                .ToList()
        });
    }

    private static async Task<IResult> SubmitAddOnRequest(
        SubmitAddOnRequestBody request,
        ITenantContext tenantContext,
        SubscriptionManager manager,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Forbid();
        }

        var entity = await manager.RequestAddOnAsync(
            tenantContext.TenantId.Value,
            request.ModuleCode,
            request.Notes,
            cancellationToken);

        return Results.Created($"/api/v1/subscriptions/addon-requests/{entity.Id}", new { entity.Id, entity.ModuleCode, entity.Status });
    }
}
