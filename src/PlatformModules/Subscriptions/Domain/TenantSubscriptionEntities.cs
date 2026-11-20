using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.PlatformModules.Subscriptions.Domain;

/// <summary>
/// Active or historical commercial subscription owned by a single tenant in the Cloud Control Plane.
/// </summary>
public sealed class Subscription : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    public const int DefaultTrialDays = 30;
    public const int PastDueGraceDays = 14;

    private readonly List<TenantModule> _modules = [];

    private Subscription()
    {
        CountryCode = string.Empty;
        CurrencyCode = string.Empty;
    }

    public Guid TenantId { get; private set; }
    public string CountryCode { get; private set; }
    public Guid? PlanId { get; private set; }
    public Guid PriceBookId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public string CurrencyCode { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? TaxRateOverride { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public DateTimeOffset? TrialEndsAtUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<TenantModule> Modules => _modules.AsReadOnly();

    public static Subscription StartTrial(
        Guid tenantId,
        Plan? plan,
        PriceBook priceBook,
        BillingCycle billingCycle,
        decimal recurringAmount,
        IReadOnlyCollection<PortalModule> entitledModules,
        DateTimeOffset now,
        int trialDays = DefaultTrialDays)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(priceBook);
        ArgumentNullException.ThrowIfNull(entitledModules);
        if (trialDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trialDays));
        }

        var endDate = now.AddDays(trialDays);
        var subscription = new Subscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CountryCode = priceBook.CountryCode,
            PlanId = plan?.Id,
            PriceBookId = priceBook.Id,
            BillingCycle = billingCycle,
            CurrencyCode = priceBook.CurrencyCode,
            Amount = decimal.Round(recurringAmount, 2, MidpointRounding.AwayFromZero),
            Status = SubscriptionStatus.Trial,
            StartDate = now,
            EndDate = endDate,
            TrialEndsAtUtc = endDate
        };

        subscription.SyncModules(entitledModules, endDate, TenantModuleStatus.Active);
        return subscription;
    }

    public static Subscription CreatePaid(
        Guid tenantId,
        Plan? plan,
        PriceBook priceBook,
        BillingCycle billingCycle,
        decimal amount,
        IReadOnlyCollection<PortalModule> entitledModules,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(priceBook);
        ArgumentNullException.ThrowIfNull(entitledModules);

        var endDate = AddPeriod(now, billingCycle);
        var subscription = new Subscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CountryCode = priceBook.CountryCode,
            PlanId = plan?.Id,
            PriceBookId = priceBook.Id,
            BillingCycle = billingCycle,
            CurrencyCode = priceBook.CurrencyCode,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            Status = SubscriptionStatus.Active,
            StartDate = now,
            EndDate = endDate
        };

        subscription.SyncModules(entitledModules, endDate, TenantModuleStatus.Active);
        return subscription;
    }

    public void Reconfigure(
        Plan? plan,
        PriceBook priceBook,
        decimal newAmount,
        IReadOnlyCollection<PortalModule> modules,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(priceBook);
        ArgumentNullException.ThrowIfNull(modules);

        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot reconfigure a cancelled subscription.");
        }

        if (!string.Equals(priceBook.CurrencyCode, CurrencyCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PriceBook currency must match the existing subscription currency.");
        }

        PlanId = plan?.Id;
        PriceBookId = priceBook.Id;
        Amount = decimal.Round(newAmount, 2, MidpointRounding.AwayFromZero);

        var targetModuleStatus = Status == SubscriptionStatus.Suspended
            ? TenantModuleStatus.Suspended
            : TenantModuleStatus.Active;

        SyncModules(modules, EndDate > now ? EndDate : now, targetModuleStatus);
    }

    public void SetTaxRateOverride(decimal? taxRate)
    {
        if (taxRate is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate override must be between 0 and 100.");
        }

        TaxRateOverride = taxRate.HasValue ? decimal.Round(taxRate.Value, 2, MidpointRounding.AwayFromZero) : null;
    }

    public void ApplyPaidPeriod(DateTimeOffset periodEnd)
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot apply payment to a cancelled subscription.");
        }

        if (periodEnd > EndDate)
        {
            EndDate = periodEnd;
        }

        Status = SubscriptionStatus.Active;
        SuspendedAtUtc = null;

        foreach (var module in _modules)
        {
            if (module.Status != TenantModuleStatus.Expired || periodEnd > module.ValidTo)
            {
                module.ActivateUntil(EndDate);
            }
        }
    }

    public void MarkPastDue(DateTimeOffset now)
    {
        if (Status is SubscriptionStatus.Trial or SubscriptionStatus.Active && EndDate <= now)
        {
            Status = SubscriptionStatus.PastDue;
        }
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Suspended)
        {
            return;
        }

        Status = SubscriptionStatus.Suspended;
        SuspendedAtUtc = now;

        foreach (var module in _modules.Where(m => !m.ModuleCode.IsCoreModule()))
        {
            module.Suspend();
        }
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            return;
        }

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = now;
        foreach (var module in _modules.Where(m => !m.ModuleCode.IsCoreModule()))
        {
            module.Expire();
        }
    }

    public static DateTimeOffset RenewalStart(DateTimeOffset currentEndDate, DateTimeOffset now) =>
        currentEndDate > now ? currentEndDate : now;

    public static DateTimeOffset AddPeriod(DateTimeOffset start, BillingCycle cycle) =>
        cycle == BillingCycle.Annual ? start.AddYears(1) : start.AddMonths(1);

    private void SyncModules(
        IReadOnlyCollection<PortalModule> targetModules,
        DateTimeOffset validTo,
        TenantModuleStatus activeStatus)
    {
        var desiredById = targetModules.ToDictionary(m => m.Id);

        foreach (var existing in _modules.ToList())
        {
            if (desiredById.ContainsKey(existing.ModuleId))
            {
                if (activeStatus == TenantModuleStatus.Suspended && !existing.ModuleCode.IsCoreModule())
                {
                    existing.Suspend();
                }
                else
                {
                    existing.ActivateUntil(validTo);
                }

                desiredById.Remove(existing.ModuleId);
            }
            else if (!existing.ModuleCode.IsCoreModule())
            {
                existing.Expire();
            }
        }

        foreach (var added in desiredById.Values)
        {
            var initialStatus = activeStatus == TenantModuleStatus.Suspended && !added.IsCore
                ? TenantModuleStatus.Suspended
                : TenantModuleStatus.Active;

            _modules.Add(TenantModule.Create(TenantId, Id, added.Id, added.Code, validTo, initialStatus));
        }
    }
}

/// <summary>
/// Per-tenant entitlement row controlled by the tenant's <see cref="Subscription"/>.
/// </summary>
public sealed class TenantModule : Entity<Guid>, ITenantScopedEntity
{
    private TenantModule()
    {
    }

    private TenantModule(
        Guid tenantId,
        Guid subscriptionId,
        Guid moduleId,
        ModuleCode moduleCode,
        DateTimeOffset validTo,
        TenantModuleStatus status)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        ModuleId = moduleId;
        ModuleCode = moduleCode;
        IsSchoolWide = true;
        Status = status;
        ValidTo = validTo;
    }

    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid ModuleId { get; private set; }
    public ModuleCode ModuleCode { get; private set; }
    public bool IsSchoolWide { get; private set; }
    public Guid? CampusId { get; private set; }
    public TenantModuleStatus Status { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }

    public static TenantModule Create(
        Guid tenantId,
        Guid subscriptionId,
        Guid moduleId,
        ModuleCode moduleCode,
        DateTimeOffset validTo,
        TenantModuleStatus status = TenantModuleStatus.Active)
        => new(tenantId, subscriptionId, moduleId, moduleCode, validTo, status);

    public void ActivateUntil(DateTimeOffset validTo)
    {
        Status = TenantModuleStatus.Active;
        ValidTo = validTo;
    }

    public void Suspend() => Status = TenantModuleStatus.Suspended;
    public void Expire() => Status = TenantModuleStatus.Expired;
}

/// <summary>
/// Issued tenant billing invoice backed by immutable line items and recorded payments.
/// </summary>
public sealed class Invoice : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    public const int NumberMaxLength = 40;
    public const int DefaultPaymentTermDays = 14;

    private readonly List<InvoiceLine> _lines = [];
    private readonly List<Payment> _payments = [];

    private Invoice()
    {
        InvoiceNumber = string.Empty;
        CurrencyCode = string.Empty;
    }

    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public string CurrencyCode { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public static Invoice Create(
        Subscription subscription,
        string invoiceNumber,
        decimal taxRate,
        DateTimeOffset now,
        bool nextPeriod,
        string description)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (subscription.Status == SubscriptionStatus.Trial && !nextPeriod)
        {
            throw new InvalidOperationException("A trial subscription can only be invoiced for the next period.");
        }

        var periodStart = nextPeriod
            ? Subscription.RenewalStart(subscription.EndDate, now)
            : subscription.StartDate;
        var periodEnd = Subscription.AddPeriod(periodStart, subscription.BillingCycle);

        var subtotal = decimal.Round(subscription.Amount, 2, MidpointRounding.AwayFromZero);
        var normalizedTaxRate = decimal.Round(taxRate, 2, MidpointRounding.AwayFromZero);
        var taxAmount = decimal.Round(subtotal * (normalizedTaxRate / 100m), 2, MidpointRounding.AwayFromZero);
        var total = subtotal + taxAmount;

        var invoice = new Invoice
        {
            Id = Guid.CreateVersion7(),
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            InvoiceNumber = invoiceNumber.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CurrencyCode = subscription.CurrencyCode,
            Subtotal = subtotal,
            TaxRate = normalizedTaxRate,
            TaxAmount = taxAmount,
            Total = total,
            Status = InvoiceStatus.Open,
            IssuedAtUtc = now,
            DueAtUtc = now.AddDays(DefaultPaymentTermDays)
        };

        invoice._lines.Add(InvoiceLine.Create(subscription.TenantId, invoice.Id, description.Trim(), subtotal));
        return invoice;
    }

    public Payment RecordPayment(
        decimal amount,
        PaymentMethod method,
        string reference,
        DateTimeOffset receivedAtUtc,
        string providerName)
    {
        if (Status != InvoiceStatus.Open)
        {
            throw new InvalidOperationException("Payments can only be recorded on open invoices.");
        }

        var normalizedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (normalizedAmount != Total)
        {
            throw new InvalidOperationException($"Partial or over-payments are not permitted. Expected {Total:F2} {CurrencyCode}.");
        }

        var payment = Payment.Create(TenantId, Id, normalizedAmount, method, reference, providerName, receivedAtUtc);
        _payments.Add(payment);
        Status = InvoiceStatus.Paid;
        PaidAtUtc = receivedAtUtc;
        return payment;
    }

    public void Void(DateTimeOffset now)
    {
        if (Status != InvoiceStatus.Open)
        {
            throw new InvalidOperationException("Only open invoices can be voided.");
        }

        Status = InvoiceStatus.Void;
        VoidedAtUtc = now;
    }
}

public sealed class InvoiceLine : Entity<Guid>, ITenantScopedEntity
{
    public const int DescriptionMaxLength = 250;

    private InvoiceLine()
    {
        Description = string.Empty;
    }

    private InvoiceLine(Guid tenantId, Guid invoiceId, string description, decimal amount) : this()
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        InvoiceId = invoiceId;
        Description = description.Length > DescriptionMaxLength ? description[..DescriptionMaxLength] : description;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public Guid TenantId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }

    public static InvoiceLine Create(Guid tenantId, Guid invoiceId, string description, decimal amount) =>
        new(tenantId, invoiceId, description, amount);
}

public sealed class Payment : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    public const int ReferenceMaxLength = 120;
    public const int ProviderMaxLength = 64;

    private Payment()
    {
        Reference = string.Empty;
        ProviderName = string.Empty;
    }

    private Payment(
        Guid tenantId,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        string reference,
        string providerName,
        DateTimeOffset receivedAtUtc) : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var trimmedRef = reference.Trim();
        if (trimmedRef.Length > ReferenceMaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        InvoiceId = invoiceId;
        Amount = amount;
        Method = method;
        Reference = trimmedRef;
        ProviderName = providerName.Trim();
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string Reference { get; private set; }
    public string ProviderName { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static Payment Create(
        Guid tenantId,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        string reference,
        string providerName,
        DateTimeOffset receivedAtUtc)
        => new(tenantId, invoiceId, amount, method, reference, providerName, receivedAtUtc);
}

public sealed class AddOnRequest : Entity<Guid>, ITenantScopedEntity
{
    public const int NotesMaxLength = 500;

    private AddOnRequest()
    {
        RequestedByUserId = string.Empty;
    }

    private AddOnRequest(
        Guid tenantId,
        Guid moduleId,
        ModuleCode moduleCode,
        string requestedByUserId,
        string? notes,
        DateTimeOffset requestedAtUtc) : this()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserId);
        var trimmedNotes = notes?.Trim();
        if (trimmedNotes is { Length: > NotesMaxLength })
        {
            throw new ArgumentOutOfRangeException(nameof(notes));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        ModuleId = moduleId;
        ModuleCode = moduleCode;
        RequestedByUserId = requestedByUserId.Trim();
        Notes = trimmedNotes;
        Status = AddOnRequestStatus.Pending;
        RequestedAtUtc = requestedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid ModuleId { get; private set; }
    public ModuleCode ModuleCode { get; private set; }
    public string RequestedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public AddOnRequestStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? DecidedByUserId { get; private set; }

    public static AddOnRequest Create(
        Guid tenantId,
        Guid moduleId,
        ModuleCode moduleCode,
        string requestedByUserId,
        string? notes,
        DateTimeOffset requestedAtUtc)
        => new(tenantId, moduleId, moduleCode, requestedByUserId, notes, requestedAtUtc);

    public void Approve(string decidedByUserId, DateTimeOffset decidedAtUtc)
    {
        if (Status != AddOnRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending add-on requests can be approved.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(decidedByUserId);
        Status = AddOnRequestStatus.Approved;
        DecidedByUserId = decidedByUserId.Trim();
        DecidedAtUtc = decidedAtUtc;
    }

    public void Reject(string decidedByUserId, DateTimeOffset decidedAtUtc)
    {
        if (Status != AddOnRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending add-on requests can be rejected.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(decidedByUserId);
        Status = AddOnRequestStatus.Rejected;
        DecidedByUserId = decidedByUserId.Trim();
        DecidedAtUtc = decidedAtUtc;
    }
}

public sealed class RenewalReminder : Entity<Guid>, ITenantScopedEntity
{
    public const int MaxDaysBefore = 365;

    private RenewalReminder()
    {
    }

    private RenewalReminder(Subscription subscription, int daysBefore, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysBefore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysBefore, MaxDaysBefore);

        Id = Guid.CreateVersion7();
        TenantId = subscription.TenantId;
        SubscriptionId = subscription.Id;
        PeriodEnd = subscription.EndDate;
        DaysBefore = daysBefore;
        QueuedAt = queuedAt;
    }

    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public int DaysBefore { get; private set; }
    public DateTimeOffset QueuedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public static RenewalReminder Queue(Subscription subscription, int daysBefore, DateTimeOffset queuedAt) =>
        new(subscription, daysBefore, queuedAt);

    public void MarkSent(DateTimeOffset sentAt) => SentAt ??= sentAt;
}
