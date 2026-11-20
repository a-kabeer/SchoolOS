using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.PlatformModules.Subscriptions.Domain;
using SchoolOS.PlatformModules.Subscriptions.Persistence;
using SchoolOS.PlatformModules.Tenants.Domain;

namespace SchoolOS.PlatformModules.Subscriptions.Application;

public sealed record SubscriptionQuote(
    Plan? Plan,
    PriceBook PriceBook,
    decimal Amount,
    IReadOnlyList<PortalModule> Modules);

public static class SubscriptionQuoteRules
{
    public static IReadOnlyList<PortalModule> ResolveModules(
        IReadOnlyList<PortalModule> catalogue,
        IReadOnlyCollection<(Guid ModuleId, Guid RequiresModuleId)> dependencies,
        IReadOnlySet<Guid> planModuleIds,
        IReadOnlyCollection<ModuleCode> addOnCodes)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(planModuleIds);
        ArgumentNullException.ThrowIfNull(addOnCodes);

        var addOns = new List<PortalModule>(addOnCodes.Count);
        foreach (var code in addOnCodes.Distinct())
        {
            var module = catalogue.FirstOrDefault(m => m.Code == code)
                ?? throw new ArgumentException($"Unknown module code '{code}'.", nameof(addOnCodes));

            if (module.IsCore)
            {
                throw new ArgumentException($"Core module '{code}' is always included and cannot be sold as an add-on.", nameof(addOnCodes));
            }

            addOns.Add(module);
        }

        var chosenIds = planModuleIds.Concat(addOns.Select(m => m.Id)).ToList();
        var grantedIds = ModuleDependencyGraph.RequiredBy(dependencies, chosenIds)
            .Concat(catalogue.Where(m => m.IsCore).Select(m => m.Id))
            .ToHashSet();

        var resolved = catalogue.Where(m => grantedIds.Contains(m.Id)).OrderBy(m => m.SortOrder).ThenBy(m => m.Code).ToList();
        var inactive = resolved.FirstOrDefault(m => !m.IsActive);
        if (inactive is not null)
        {
            throw new InvalidOperationException($"Module '{inactive.Code}' is currently inactive.");
        }

        return resolved;
    }

    public static decimal Price(
        Plan? plan,
        IReadOnlySet<Guid> planModuleIds,
        IReadOnlyCollection<PortalModule> resolvedModules,
        IReadOnlyCollection<PriceBookItem> cyclePrices)
    {
        ArgumentNullException.ThrowIfNull(planModuleIds);
        ArgumentNullException.ThrowIfNull(resolvedModules);
        ArgumentNullException.ThrowIfNull(cyclePrices);

        decimal amount = 0m;
        if (plan is not null)
        {
            var planPrice = cyclePrices.FirstOrDefault(i => i.PlanId == plan.Id)
                ?? throw new InvalidOperationException($"Price is missing for plan '{plan.Code}'.");
            amount += planPrice.Price;
        }

        foreach (var module in resolvedModules.Where(m => !m.IsCore && !planModuleIds.Contains(m.Id)))
        {
            var modulePrice = cyclePrices.FirstOrDefault(i => i.ModuleId == module.Id)
                ?? throw new InvalidOperationException($"Price is missing for add-on module '{module.Code}'.");
            amount += modulePrice.Price;
        }

        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}

public static class SubscriptionCatalogSeeder
{
    private static readonly (ModuleCode Code, string Name, string Description, bool IsCore, int SortOrder)[] DefaultModules =
    [
        (ModuleCode.School, "School Organization", "School & master institutional configuration", true, 1),
        (ModuleCode.Campus, "Campuses", "Multi-campus branch management", true, 2),
        (ModuleCode.AcademicSetup, "Academic Setup", "Academic years, sessions, classes & holidays", true, 3),
        (ModuleCode.UsersRoles, "Users & Roles", "Identity, scoped RBAC roles & security audit", true, 4),
        (ModuleCode.Students, "Students", "Student directory & core enrollment records", true, 5),
        (ModuleCode.Staff, "Staff", "Teachers & school personnel directory", true, 6),
        (ModuleCode.Admissions, "Admissions", "Enquiries, campus visits & admission conversions", false, 7),
        (ModuleCode.Attendance, "Attendance", "Daily student and staff attendance tracking", false, 8),
        (ModuleCode.Fees, "Fee Management", "Challan generation, discounts & fee ledgers", false, 9),
        (ModuleCode.ParentCommunication, "Parent Communication", "SMS, email & parent notices", false, 10),
        (ModuleCode.Exams, "Examinations", "Exam schedules, gradebooks & report cards", false, 11),
        (ModuleCode.Timetable, "Timetable", "Class routine & teacher timetable builder", false, 12),
        (ModuleCode.HrPayroll, "HR & Payroll", "Staff salaries, deductions & payslips", false, 13),
        (ModuleCode.Transport, "Transport", "School buses, routes & transport passes", false, 14),
        (ModuleCode.Library, "Library", "Book catalog, circulation & fines", false, 15),
        (ModuleCode.Lms, "Learning Management", "Digital lessons, assignments & resources", false, 16),
        (ModuleCode.Hostel, "Hostel", "Boarding rooms & hostel allocations", false, 17)
    ];

    public static async Task SeedAsync(SubscriptionsDbContext db, CancellationToken cancellationToken = default)
    {
        var existingModules = await db.PortalModules.ToListAsync(cancellationToken);
        var byCode = existingModules.ToDictionary(m => m.Code);

        foreach (var (code, name, desc, isCore, sortOrder) in DefaultModules)
        {
            if (!byCode.TryGetValue(code, out var mod))
            {
                mod = PortalModule.Create(code, name, desc, isCore, sortOrder);
                db.PortalModules.Add(mod);
                byCode[code] = mod;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var existingDeps = await db.ModuleDependencies.Select(d => new { d.ModuleId, d.RequiresModuleId }).ToListAsync(cancellationToken);
        var depSet = existingDeps.Select(d => (d.ModuleId, d.RequiresModuleId)).ToHashSet();

        (ModuleCode Child, ModuleCode Parent)[] edges =
        [
            (ModuleCode.Exams, ModuleCode.AcademicSetup),
            (ModuleCode.Timetable, ModuleCode.AcademicSetup),
            (ModuleCode.Lms, ModuleCode.AcademicSetup),
            (ModuleCode.Fees, ModuleCode.Students),
            (ModuleCode.Transport, ModuleCode.Students),
            (ModuleCode.Hostel, ModuleCode.Students),
            (ModuleCode.HrPayroll, ModuleCode.Staff)
        ];

        foreach (var (child, parent) in edges)
        {
            var cId = byCode[child].Id;
            var pId = byCode[parent].Id;
            if (!depSet.Contains((cId, pId)))
            {
                db.ModuleDependencies.Add(ModuleDependency.Create(cId, pId));
                depSet.Add((cId, pId));
            }
        }

        var existingPlans = await db.Plans.Include(p => p.Modules).ToDictionaryAsync(p => p.Code, cancellationToken);
        if (!existingPlans.ContainsKey("STARTER"))
        {
            var starter = Plan.Create("STARTER", "Starter Plan", "Core modules plus Admissions and Attendance");
            starter.AddModule(byCode[ModuleCode.Admissions]);
            starter.AddModule(byCode[ModuleCode.Attendance]);
            db.Plans.Add(starter);
        }

        if (!existingPlans.ContainsKey("STANDARD"))
        {
            var standard = Plan.Create("STANDARD", "Standard Plan", "Starter plus Fees, Exams & Timetable");
            standard.AddModule(byCode[ModuleCode.Admissions]);
            standard.AddModule(byCode[ModuleCode.Attendance]);
            standard.AddModule(byCode[ModuleCode.Fees]);
            standard.AddModule(byCode[ModuleCode.Exams]);
            standard.AddModule(byCode[ModuleCode.Timetable]);
            db.Plans.Add(standard);
        }

        if (!existingPlans.ContainsKey("FULL"))
        {
            var full = Plan.Create("FULL", "Full Campus Plan", "Every paid add-on module included");
            foreach (var mod in byCode.Values.Where(m => !m.IsCore))
            {
                full.AddModule(mod);
            }
            db.Plans.Add(full);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface IPaymentProvider
{
    string Name { get; }
    Task<(bool Succeeded, string Reference)> ChargeAsync(
        Guid invoiceId,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string reference,
        CancellationToken cancellationToken = default);
}

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public string Name => "manual";

    public Task<(bool Succeeded, string Reference)> ChargeAsync(
        Guid invoiceId,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string reference,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0m || string.IsNullOrWhiteSpace(reference))
        {
            return Task.FromResult((false, string.Empty));
        }

        return Task.FromResult((true, reference.Trim()));
    }
}

public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(SubscriptionsDbContext db, CancellationToken cancellationToken = default);
}

public sealed class SequenceInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private long _inMemorySeq;

    public async Task<string> NextAsync(SubscriptionsDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsNpgsql())
        {
            var next = await db.Database
                .SqlQuery<long>($"SELECT nextval('subscriptions.invoice_number_seq') AS \"Value\"")
                .SingleAsync(cancellationToken);
            return $"INV-{next:D6}";
        }

        var seq = Interlocked.Increment(ref _inMemorySeq);
        return $"INV-{seq:D6}";
    }
}

public sealed class SubscriptionManager
{
    private readonly SubscriptionsDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IInvoiceNumberGenerator _invoiceNumbers;
    private readonly ISecurityAuditLog _auditLog;
    private readonly TimeProvider _timeProvider;

    public SubscriptionManager(
        SubscriptionsDbContext db,
        ITenantContext tenantContext,
        IPaymentProvider paymentProvider,
        IInvoiceNumberGenerator invoiceNumbers,
        ISecurityAuditLog auditLog,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _paymentProvider = paymentProvider;
        _invoiceNumbers = invoiceNumbers;
        _auditLog = auditLog;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<SubscriptionQuote> QuoteAsync(
        string countryCode,
        string? planCode,
        IReadOnlyCollection<ModuleCode> addOnCodes,
        BillingCycle billingCycle,
        CancellationToken cancellationToken = default)
    {
        var normalizedCountry = Country.NormalizeCode(countryCode);
        var modules = await _db.PortalModules.AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Code)
            .ToListAsync(cancellationToken);

        Plan? plan = null;
        var planModuleIds = new HashSet<Guid>();
        if (!string.IsNullOrWhiteSpace(planCode))
        {
            var normalizedPlan = planCode.Trim().ToUpperInvariant();
            plan = await _db.Plans.AsNoTracking()
                .Include(p => p.Modules)
                .FirstOrDefaultAsync(p => p.Code == normalizedPlan && p.IsActive, cancellationToken)
                ?? throw new ArgumentException($"Active plan '{normalizedPlan}' was not found.", nameof(planCode));

            planModuleIds = plan.Modules.Select(m => m.ModuleId).ToHashSet();
        }

        var deps = await _db.ModuleDependencies.AsNoTracking()
            .Select(d => new ValueTuple<Guid, Guid>(d.ModuleId, d.RequiresModuleId))
            .ToListAsync(cancellationToken);

        var resolvedModules = SubscriptionQuoteRules.ResolveModules(modules, deps, planModuleIds, addOnCodes);

        var book = await _db.PriceBooks.AsNoTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.CountryCode == normalizedCountry && b.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active price book exists for country '{normalizedCountry}'.");

        var cyclePrices = book.Items.Where(i => i.BillingCycle == billingCycle).ToList();
        var amount = SubscriptionQuoteRules.Price(plan, planModuleIds, resolvedModules, cyclePrices);

        return new SubscriptionQuote(plan, book, amount, resolvedModules);
    }

    public async Task<Subscription> AssignSubscriptionAsync(
        Guid tenantId,
        string countryCode,
        string? planCode,
        IReadOnlyCollection<ModuleCode> addOnCodes,
        BillingCycle billingCycle,
        bool isTrial,
        decimal? taxRateOverride = null,
        CancellationToken cancellationToken = default)
    {
        var quote = await QuoteAsync(countryCode, planCode, addOnCodes, billingCycle, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        using var _ = EnterTargetTenantScope(tenantId);

        var existingActive = await _db.Subscriptions
            .AnyAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken);
        if (existingActive)
        {
            throw new InvalidOperationException("Tenant already has an active or non-cancelled subscription.");
        }

        var subscription = isTrial
            ? Subscription.StartTrial(tenantId, quote.Plan, quote.PriceBook, billingCycle, quote.Amount, quote.Modules, now)
            : Subscription.CreatePaid(tenantId, quote.Plan, quote.PriceBook, billingCycle, quote.Amount, quote.Modules, now);

        subscription.SetTaxRateOverride(taxRateOverride);
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.SubscriptionCreated,
            SecurityAuditOutcome.Success,
            targetType: nameof(Subscription),
            targetId: subscription.Id.ToString(),
            cancellationToken: cancellationToken);

        return subscription;
    }

    public async Task<Subscription> ReconfigureSubscriptionAsync(
        Guid tenantId,
        string? planCode,
        IReadOnlyCollection<ModuleCode> addOnCodes,
        CancellationToken cancellationToken = default)
    {
        using var _ = EnterTargetTenantScope(tenantId);

        var subscription = await _db.Subscriptions
            .Include(s => s.Modules)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken)
            ?? throw new InvalidOperationException($"No active subscription found for tenant '{tenantId}'.");

        var quote = await QuoteAsync(subscription.CountryCode, planCode, addOnCodes, subscription.BillingCycle, cancellationToken);
        subscription.Reconfigure(quote.Plan, quote.PriceBook, quote.Amount, quote.Modules, _timeProvider.GetUtcNow());

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.SubscriptionChanged,
            SecurityAuditOutcome.Success,
            targetType: nameof(Subscription),
            targetId: subscription.Id.ToString(),
            cancellationToken: cancellationToken);

        return subscription;
    }

    public async Task<Invoice> CreateInvoiceAsync(
        Guid tenantId,
        bool nextPeriod,
        CancellationToken cancellationToken = default)
    {
        using var _ = EnterTargetTenantScope(tenantId);

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken)
            ?? throw new InvalidOperationException($"No active subscription found for tenant '{tenantId}'.");

        if (subscription.Status == SubscriptionStatus.Trial && !nextPeriod)
        {
            throw new InvalidOperationException("Trial subscriptions can only be invoiced for the next period.");
        }

        if (await _db.Invoices.AnyAsync(i => i.SubscriptionId == subscription.Id && i.Status == InvoiceStatus.Open, cancellationToken))
        {
            throw new InvalidOperationException("Subscription already has an open invoice.");
        }

        var now = _timeProvider.GetUtcNow();
        var periodStart = nextPeriod
            ? Subscription.RenewalStart(subscription.EndDate, now)
            : subscription.StartDate;

        if (await _db.Invoices.AnyAsync(i => i.SubscriptionId == subscription.Id && i.PeriodStart == periodStart && i.Status != InvoiceStatus.Void, cancellationToken))
        {
            throw new InvalidOperationException("The requested billing period has already been invoiced.");
        }

        var priceBookTax = await _db.PriceBooks.AsNoTracking()
            .Where(b => b.Id == subscription.PriceBookId)
            .Select(b => b.TaxRate)
            .FirstAsync(cancellationToken);

        var effectiveTax = subscription.TaxRateOverride ?? priceBookTax;
        var planCode = subscription.PlanId.HasValue
            ? await _db.Plans.AsNoTracking().Where(p => p.Id == subscription.PlanId.Value).Select(p => p.Code).FirstOrDefaultAsync(cancellationToken)
            : null;

        var invoiceNumber = await _invoiceNumbers.NextAsync(_db, cancellationToken);
        var description = $"{planCode ?? "CUSTOM"} ({subscription.BillingCycle})";
        var invoice = Invoice.Create(subscription, invoiceNumber, effectiveTax, now, nextPeriod, description);

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.InvoiceCreated,
            SecurityAuditOutcome.Success,
            targetType: nameof(Invoice),
            targetId: invoice.Id.ToString(),
            cancellationToken: cancellationToken);

        return invoice;
    }

    public async Task<Payment> RecordPaymentAsync(
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        string reference,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (receivedAtUtc > now)
        {
            throw new ArgumentException("Payment received date cannot be in the future.", nameof(receivedAtUtc));
        }

        var invoiceHeader = await _db.Invoices.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Resolve TenantId before entering target tenant scope
            .IgnoreQueryFilters()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.TenantId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' was not found.");

        using var _ = EnterTargetTenantScope(invoiceHeader.TenantId);

        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' was not found.");

        var (succeeded, verifiedRef) = await _paymentProvider.ChargeAsync(
            invoice.Id,
            amount,
            invoice.CurrencyCode,
            method,
            reference,
            cancellationToken);

        if (!succeeded)
        {
            throw new InvalidOperationException("Payment provider rejected the payment transaction.");
        }

        var payment = invoice.RecordPayment(amount, method, verifiedRef, receivedAtUtc, _paymentProvider.Name);
        _db.Payments.Add(payment);

        var subscription = await _db.Subscriptions
            .Include(s => s.Modules)
            .FirstAsync(s => s.Id == invoice.SubscriptionId, cancellationToken);

        subscription.ApplyPaidPeriod(invoice.PeriodEnd);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.PaymentRecorded,
            SecurityAuditOutcome.Success,
            targetType: nameof(Payment),
            targetId: payment.Id.ToString(),
            cancellationToken: cancellationToken);

        return payment;
    }

    public async Task VoidInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var header = await _db.Invoices.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Resolve TenantId before entering target tenant scope
            .IgnoreQueryFilters()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.TenantId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' was not found.");

        using var _ = EnterTargetTenantScope(header.TenantId);

        var invoice = await _db.Invoices.FirstAsync(i => i.Id == invoiceId, cancellationToken);
        invoice.Void(_timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.InvoiceVoided,
            SecurityAuditOutcome.Success,
            targetType: nameof(Invoice),
            targetId: invoice.Id.ToString(),
            cancellationToken: cancellationToken);
    }

    public async Task<AddOnRequest> RequestAddOnAsync(
        Guid tenantId,
        ModuleCode moduleCode,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var module = await _db.PortalModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == moduleCode && m.IsActive, cancellationToken)
            ?? throw new ArgumentException($"Active module '{moduleCode}' was not found.", nameof(moduleCode));

        if (module.IsCore)
        {
            throw new ArgumentException("Core modules do not require an add-on request.", nameof(moduleCode));
        }

        using var _ = EnterTargetTenantScope(tenantId);

        var request = AddOnRequest.Create(
            tenantId,
            module.Id,
            module.Code,
            _tenantContext.UserId ?? "tenant-admin",
            notes,
            _timeProvider.GetUtcNow());

        _db.AddOnRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.AddOnRequested,
            SecurityAuditOutcome.Success,
            targetType: nameof(AddOnRequest),
            targetId: request.Id.ToString(),
            cancellationToken: cancellationToken);

        return request;
    }

    public async Task<AddOnRequest> ApproveAddOnRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var header = await _db.AddOnRequests.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Resolve TenantId before entering target tenant scope
            .IgnoreQueryFilters()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.TenantId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Add-on request '{requestId}' was not found.");

        using var _ = EnterTargetTenantScope(header.TenantId);

        var addOnRequest = await _db.AddOnRequests.FirstAsync(r => r.Id == requestId, cancellationToken);
        var subscription = await _db.Subscriptions
            .Include(s => s.Modules)
            .FirstOrDefaultAsync(s => s.TenantId == addOnRequest.TenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken)
            ?? throw new InvalidOperationException("Tenant has no active subscription.");

        string? planCode = null;
        var planModuleIds = new HashSet<Guid>();
        if (subscription.PlanId.HasValue)
        {
            var plan = await _db.Plans.AsNoTracking()
                .Include(p => p.Modules)
                .FirstOrDefaultAsync(p => p.Id == subscription.PlanId.Value, cancellationToken);
            planCode = plan?.Code;
            if (plan is not null)
            {
                planModuleIds = plan.Modules.Select(m => m.ModuleId).ToHashSet();
            }
        }

        var currentAddOns = subscription.Modules
            .Where(m => m.Status != TenantModuleStatus.Expired && !m.ModuleCode.IsCoreModule() && !planModuleIds.Contains(m.ModuleId))
            .Select(m => m.ModuleCode)
            .ToHashSet();
        currentAddOns.Add(addOnRequest.ModuleCode);

        var quote = await QuoteAsync(subscription.CountryCode, planCode, currentAddOns, subscription.BillingCycle, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        subscription.Reconfigure(quote.Plan, quote.PriceBook, quote.Amount, quote.Modules, now);
        addOnRequest.Approve(_tenantContext.UserId ?? "platform-admin", now);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.AddOnRequestDecided,
            SecurityAuditOutcome.Success,
            targetType: nameof(AddOnRequest),
            targetId: addOnRequest.Id.ToString(),
            cancellationToken: cancellationToken);

        return addOnRequest;
    }

    public async Task<AddOnRequest> RejectAddOnRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var header = await _db.AddOnRequests.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Resolve TenantId before entering target tenant scope
            .IgnoreQueryFilters()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.TenantId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Add-on request '{requestId}' was not found.");

        using var _ = EnterTargetTenantScope(header.TenantId);

        var addOnRequest = await _db.AddOnRequests.FirstAsync(r => r.Id == requestId, cancellationToken);
        addOnRequest.Reject(_tenantContext.UserId ?? "platform-admin", _timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            SecurityAuditEventType.AddOnRequestDecided,
            SecurityAuditOutcome.Success,
            targetType: nameof(AddOnRequest),
            targetId: addOnRequest.Id.ToString(),
            cancellationToken: cancellationToken);

        return addOnRequest;
    }

    private IDisposable EnterTargetTenantScope(Guid targetTenantId)
    {
        if (_tenantContext.TenantId == targetTenantId)
        {
            return NoOpDisposable.Instance;
        }

        if (_tenantContext is TenantContext mutable && mutable.IsPlatformAdmin)
        {
            return mutable.BeginPlatformTenantScope(targetTenantId);
        }

        throw new UnauthorizedAccessException("Caller is not authorized to mutate the target tenant subscription.");
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }
}

public sealed class SubscriptionTrialProvisioner(
    SubscriptionsDbContext db,
    SubscriptionManager manager) : ITenantTrialProvisioner
{
    public async Task<Guid?> StartTrialAsync(
        Guid tenantId,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        await SubscriptionCatalogSeeder.SeedAsync(db, cancellationToken);

        var normalizedCountry = Country.NormalizeCode(countryCode);
        var hasBook = await db.PriceBooks.AsNoTracking()
            .AnyAsync(b => b.CountryCode == normalizedCountry && b.IsActive, cancellationToken);

        if (!hasBook)
        {
            return null;
        }

        var sub = await manager.AssignSubscriptionAsync(
            tenantId,
            normalizedCountry,
            planCode: "STARTER",
            addOnCodes: Array.Empty<ModuleCode>(),
            billingCycle: BillingCycle.Monthly,
            isTrial: true,
            cancellationToken: cancellationToken);

        return sub.Id;
    }
}

public sealed class SubscriptionModuleAccess(SubscriptionsDbContext db, TimeProvider? timeProvider = null) : IModuleAccess
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<bool> HasModuleAsync(Guid tenantId, ModuleCode module, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return false;
        }

        if (module == ModuleCode.None || module.IsCoreModule())
        {
            return true;
        }

        var now = _timeProvider.GetUtcNow();
        return await db.TenantModules.AsNoTracking()
            .AnyAsync(
                m => m.TenantId == tenantId &&
                     m.ModuleCode == module &&
                     m.Status == TenantModuleStatus.Active &&
                     m.ValidTo > now,
                cancellationToken);
    }

    public async ValueTask<IReadOnlySet<ModuleCode>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var enabled = new HashSet<ModuleCode>(OfflineLicenseLeaseVerifier.CoreModules);
        if (tenantId == Guid.Empty)
        {
            return enabled;
        }

        var now = _timeProvider.GetUtcNow();
        var active = await db.TenantModules.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Status == TenantModuleStatus.Active && m.ValidTo > now)
            .Select(m => m.ModuleCode)
            .ToListAsync(cancellationToken);

        foreach (var code in active)
        {
            enabled.Add(code);
        }

        return enabled;
    }
}

public sealed record LifecycleSweepResult(int QueuedReminders, int MarkedPastDue, int Suspended);

public sealed class SubscriptionLifecycleService
{
    private static readonly int[] ReminderDays = [30, 7, 1];
    private readonly SubscriptionsDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public SubscriptionLifecycleService(
        SubscriptionsDbContext db,
        ITenantContext tenantContext,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<LifecycleSweepResult> RunSweepAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var tenantIds = await _db.Subscriptions.AsNoTracking()
            // compliance-exception: SEC-AUTHZ-03 Cloud Control Plane lifecycle worker cross-tenant sweep backed by schoolos_platform_admin RLS role
            .IgnoreQueryFilters()
            .Where(s => s.Status != SubscriptionStatus.Cancelled && s.Status != SubscriptionStatus.Suspended)
            .Select(s => s.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int queued = 0;
        int pastDue = 0;
        int suspended = 0;

        foreach (var tenantId in tenantIds)
        {
            using var _ = BeginTenantScope(tenantId);

            var sub = await _db.Subscriptions
                .Include(s => s.Modules)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled, cancellationToken);
            if (sub is null)
            {
                continue;
            }

            if (sub.Status is SubscriptionStatus.Trial or SubscriptionStatus.Active)
            {
                var remainingDays = (sub.EndDate - now).TotalDays;
                foreach (var threshold in ReminderDays)
                {
                    if (remainingDays > 0 && remainingDays <= threshold)
                    {
                        var exists = await _db.RenewalReminders.AnyAsync(
                            r => r.SubscriptionId == sub.Id &&
                                 r.PeriodEnd == sub.EndDate &&
                                 r.DaysBefore == threshold,
                            cancellationToken);

                        if (!exists)
                        {
                            _db.RenewalReminders.Add(RenewalReminder.Queue(sub, threshold, now));
                            queued++;
                        }
                    }
                }
            }

            if (sub.Status == SubscriptionStatus.Trial && sub.EndDate <= now)
            {
                sub.Suspend(now);
                suspended++;
            }
            else if (sub.Status == SubscriptionStatus.Active && sub.EndDate <= now)
            {
                sub.MarkPastDue(now);
                pastDue++;
            }

            if (sub.Status == SubscriptionStatus.PastDue && sub.EndDate.AddDays(Subscription.PastDueGraceDays) <= now)
            {
                sub.Suspend(now);
                suspended++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return new LifecycleSweepResult(queued, pastDue, suspended);
    }

    private IDisposable BeginTenantScope(Guid tenantId)
    {
        if (_tenantContext.TenantId == tenantId)
        {
            return NoOpScope.Instance;
        }

        if (_tenantContext is TenantContext mutable && mutable.IsPlatformAdmin)
        {
            return mutable.BeginPlatformTenantScope(tenantId);
        }

        throw new UnauthorizedAccessException("Lifecycle service requires PlatformAdmin context.");
    }

    private sealed class NoOpScope : IDisposable
    {
        public static readonly NoOpScope Instance = new();
        public void Dispose() { }
    }
}

public sealed class SubscriptionLifecycleBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionLifecycleBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                if (ctx is TenantContext mutable && !mutable.IsAuthenticated)
                {
                    mutable.EnterSystemJobScope("system:subscription-lifecycle", isPlatformWorker: true);
                }

                var svc = scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleService>();
                var result = await svc.RunSweepAsync(stoppingToken);
                logger.LogInformation(
                    "Subscription lifecycle sweep finished: {Queued} queued reminders, {PastDue} past-due, {Suspended} suspended.",
                    result.QueuedReminders,
                    result.MarkedPastDue,
                    result.Suspended);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Subscription lifecycle sweep encountered an error.");
            }
        }
    }
}
