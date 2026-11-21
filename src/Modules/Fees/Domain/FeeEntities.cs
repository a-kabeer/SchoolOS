using System.Text.RegularExpressions;
using SchoolOS.BuildingBlocks.Persistence.Audit;
using SchoolOS.BuildingBlocks.SharedKernel;

namespace SchoolOS.Modules.Fees.Domain;

public enum FeeFrequency
{
    Monthly = 1,
    Term = 2,
    Annual = 3,
    OneTime = 4
}

public enum ChallanStatus
{
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Voided = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3,
    OnlineGateway = 4
}

public enum ReceiptStatus
{
    Posted = 1,
    Reversed = 2
}

/// <summary>
/// School-level fee head definition (e.g., TUITION, LAB, ADMISSION) owned by <c>fees.fee_heads</c>.
/// </summary>
public sealed class FeeHead : Entity<Guid>, ITenantScopedEntity, IChangeAuditedEntity
{
    private FeeHead()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private FeeHead(Guid tenantId, Guid schoolId, string code, string name, FeeFrequency frequency, bool isOptional) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Frequency = frequency;
        IsOptional = isOptional;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public FeeFrequency Frequency { get; private set; }
    public bool IsOptional { get; private set; }
    public bool IsActive { get; private set; }

    public static FeeHead Create(Guid tenantId, Guid schoolId, string code, string name, FeeFrequency frequency, bool isOptional = false)
        => new(tenantId, schoolId, code, name, frequency, isOptional);
}

/// <summary>
/// Campus + AcademicYear + GradeLevel fee schedule with optional multi-sibling household discount (<c>fees.fee_schedules</c>).
/// </summary>
public sealed class FeeSchedule : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private FeeSchedule()
    {
        CurrencyCode = "PKR";
    }

    private FeeSchedule(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid feeHeadId,
        decimal amount,
        string currencyCode,
        decimal siblingDiscountPercent,
        decimal lateFeePerDay) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (academicYearId == Guid.Empty) throw new ArgumentException("Academic year ID cannot be empty.", nameof(academicYearId));
        if (gradeLevelId == Guid.Empty) throw new ArgumentException("Grade level ID cannot be empty.", nameof(gradeLevelId));
        if (feeHeadId == Guid.Empty) throw new ArgumentException("Fee head ID cannot be empty.", nameof(feeHeadId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Schedule amount must be greater than zero.");
        if (siblingDiscountPercent is < 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(siblingDiscountPercent), "Sibling discount must be between 0 and 100.");
        if (lateFeePerDay < 0m) throw new ArgumentOutOfRangeException(nameof(lateFeePerDay), "Late fee per day cannot be negative.");

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        FeeHeadId = feeHeadId;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "PKR" : currencyCode.Trim().ToUpperInvariant();
        SiblingDiscountPercent = Math.Round(siblingDiscountPercent, 2, MidpointRounding.AwayFromZero);
        LateFeePerDay = Math.Round(lateFeePerDay, 2, MidpointRounding.AwayFromZero);
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public Guid FeeHeadId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; }
    public decimal SiblingDiscountPercent { get; private set; }
    public decimal LateFeePerDay { get; private set; }
    public bool IsActive { get; private set; }

    public static FeeSchedule Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid feeHeadId,
        decimal amount,
        string currencyCode = "PKR",
        decimal siblingDiscountPercent = 0m,
        decimal lateFeePerDay = 0m)
        => new(tenantId, schoolId, campusId, academicYearId, gradeLevelId, feeHeadId, amount, currencyCode, siblingDiscountPercent, lateFeePerDay);

    public decimal CalculateDiscountForSiblingCount(int activeHouseholdSiblingCount)
    {
        if (activeHouseholdSiblingCount < 2 || SiblingDiscountPercent <= 0m)
        {
            return 0m;
        }

        return Math.Round(Amount * (SiblingDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Monthly/periodic student fee challan aggregate (<c>fees.fee_challans</c>).
/// Enforces exact balance invariants: no overpayment, no payment on voided challans, no voiding when payments exist.
/// </summary>
public sealed partial class FeeChallan : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private readonly List<FeeChallanLine> _lines = [];

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.CultureInvariant)]
    private static partial Regex BillingPeriodRegex();

    private FeeChallan()
    {
        BillingPeriod = string.Empty;
        ChallanNumber = string.Empty;
        CurrencyCode = "PKR";
    }

    private FeeChallan(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid studentId,
        Guid? householdId,
        Guid academicYearId,
        Guid gradeLevelId,
        string billingPeriod,
        string challanNumber,
        string currencyCode,
        DateOnly issueDate,
        DateOnly dueDate,
        IEnumerable<FeeChallanLine> lines) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        if (!BillingPeriodRegex().IsMatch(billingPeriod ?? string.Empty))
        {
            throw new ArgumentException("Billing period must be in YYYY-MM format (e.g. 2026-11).", nameof(billingPeriod));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(challanNumber);
        if (dueDate < issueDate)
        {
            throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(dueDate));
        }

        var lineList = lines.ToList();
        if (lineList.Count == 0)
        {
            throw new ArgumentException("A fee challan must contain at least one fee line.", nameof(lines));
        }

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        StudentId = studentId;
        HouseholdId = householdId;
        AcademicYearId = academicYearId;
        GradeLevelId = gradeLevelId;
        BillingPeriod = billingPeriod!.Trim();
        ChallanNumber = challanNumber.Trim().ToUpperInvariant();
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "PKR" : currencyCode.Trim().ToUpperInvariant();
        IssueDate = issueDate;
        DueDate = dueDate;

        foreach (var line in lineList)
        {
            line.AttachToChallan(Id);
            _lines.Add(line);
        }

        GrossAmount = Math.Round(_lines.Sum(l => l.GrossAmount), 2, MidpointRounding.AwayFromZero);
        DiscountAmount = Math.Round(_lines.Sum(l => l.DiscountAmount), 2, MidpointRounding.AwayFromZero);
        LateFeeAmount = 0m;
        NetAmount = Math.Round(_lines.Sum(l => l.NetAmount), 2, MidpointRounding.AwayFromZero);
        PaidAmount = 0m;
        BalanceAmount = NetAmount;
        Status = NetAmount == 0m ? ChallanStatus.Paid : ChallanStatus.Issued;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public Guid GradeLevelId { get; private set; }
    public string BillingPeriod { get; private set; }
    public string ChallanNumber { get; private set; }
    public string CurrencyCode { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LateFeeAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal BalanceAmount { get; private set; }
    public ChallanStatus Status { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<FeeChallanLine> Lines => _lines.AsReadOnly();

    public static FeeChallan Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid studentId,
        Guid? householdId,
        Guid academicYearId,
        Guid gradeLevelId,
        string billingPeriod,
        string challanNumber,
        string currencyCode,
        DateOnly issueDate,
        DateOnly dueDate,
        IEnumerable<FeeChallanLine> lines)
        => new(tenantId, schoolId, campusId, studentId, householdId, academicYearId, gradeLevelId, billingPeriod, challanNumber, currencyCode, issueDate, dueDate, lines);

    public void ApplyPayment(decimal amount)
    {
        if (Status == ChallanStatus.Voided)
        {
            throw new InvalidOperationException("Cannot collect payment against a voided challan.");
        }

        if (Status == ChallanStatus.Paid || BalanceAmount <= 0m)
        {
            throw new InvalidOperationException("Challan is already fully paid.");
        }

        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        if (rounded > BalanceAmount)
        {
            throw new InvalidOperationException($"Payment amount ({rounded:F2}) exceeds remaining balance ({BalanceAmount:F2}).");
        }

        PaidAmount = Math.Round(PaidAmount + rounded, 2, MidpointRounding.AwayFromZero);
        BalanceAmount = Math.Round(NetAmount + LateFeeAmount - PaidAmount, 2, MidpointRounding.AwayFromZero);
        Status = BalanceAmount == 0m ? ChallanStatus.Paid : ChallanStatus.PartiallyPaid;
    }

    public void ReversePayment(decimal amount)
    {
        if (Status == ChallanStatus.Voided)
        {
            throw new InvalidOperationException("Cannot reverse payment on a voided challan.");
        }

        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0m || rounded > PaidAmount)
        {
            throw new InvalidOperationException("Reversal amount exceeds paid amount on this challan.");
        }

        PaidAmount = Math.Round(PaidAmount - rounded, 2, MidpointRounding.AwayFromZero);
        BalanceAmount = Math.Round(NetAmount + LateFeeAmount - PaidAmount, 2, MidpointRounding.AwayFromZero);
        Status = PaidAmount == 0m ? ChallanStatus.Issued : ChallanStatus.PartiallyPaid;
    }

    public void Void(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == ChallanStatus.Voided)
        {
            throw new InvalidOperationException("Challan is already voided.");
        }

        if (PaidAmount > 0m)
        {
            throw new InvalidOperationException("Cannot void a challan that has posted payments. Reverse receipts first.");
        }

        Status = ChallanStatus.Voided;
        BalanceAmount = 0m;
        VoidReason = reason.Trim();
    }
}

public sealed class FeeChallanLine : Entity<Guid>, ITenantScopedEntity
{
    private FeeChallanLine()
    {
        Description = string.Empty;
    }

    private FeeChallanLine(Guid tenantId, Guid feeHeadId, string description, decimal grossAmount, decimal discountAmount) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (feeHeadId == Guid.Empty) throw new ArgumentException("Fee head ID cannot be empty.", nameof(feeHeadId));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (grossAmount < 0m) throw new ArgumentOutOfRangeException(nameof(grossAmount));
        if (discountAmount < 0m || discountAmount > grossAmount) throw new ArgumentOutOfRangeException(nameof(discountAmount));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        FeeHeadId = feeHeadId;
        Description = description.Trim();
        GrossAmount = Math.Round(grossAmount, 2, MidpointRounding.AwayFromZero);
        DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        NetAmount = Math.Round(GrossAmount - DiscountAmount, 2, MidpointRounding.AwayFromZero);
    }

    public Guid TenantId { get; private set; }
    public Guid ChallanId { get; private set; }
    public Guid FeeHeadId { get; private set; }
    public string Description { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }

    public static FeeChallanLine Create(Guid tenantId, Guid feeHeadId, string description, decimal grossAmount, decimal discountAmount = 0m)
        => new(tenantId, feeHeadId, description, grossAmount, discountAmount);

    internal void AttachToChallan(Guid challanId) => ChallanId = challanId;
}

/// <summary>
/// Immutable financial payment receipt (<c>fees.fee_receipts</c>).
/// Posted receipts are never deleted or edited in place; reversals are recorded explicitly with reason, actor, and timestamp.
/// </summary>
public sealed class FeeReceipt : Entity<Guid>, ICampusScopedEntity, IChangeAuditedEntity
{
    private FeeReceipt()
    {
        ReceiptNumber = string.Empty;
        CurrencyCode = "PKR";
        CollectedBy = string.Empty;
    }

    private FeeReceipt(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid challanId,
        Guid studentId,
        string receiptNumber,
        DateOnly paymentDate,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string? externalReference,
        string collectedBy,
        DateTimeOffset collectedAtUtc) : this()
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (schoolId == Guid.Empty) throw new ArgumentException("School ID cannot be empty.", nameof(schoolId));
        if (campusId == Guid.Empty) throw new ArgumentException("Campus ID cannot be empty.", nameof(campusId));
        if (challanId == Guid.Empty) throw new ArgumentException("Challan ID cannot be empty.", nameof(challanId));
        if (studentId == Guid.Empty) throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Receipt amount must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(collectedBy);

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        SchoolId = schoolId;
        CampusId = campusId;
        ChallanId = challanId;
        StudentId = studentId;
        ReceiptNumber = receiptNumber.Trim().ToUpperInvariant();
        PaymentDate = paymentDate;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "PKR" : currencyCode.Trim().ToUpperInvariant();
        Method = method;
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        CollectedBy = collectedBy.Trim();
        CollectedAtUtc = collectedAtUtc;
        Status = ReceiptStatus.Posted;
    }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid ChallanId { get; private set; }
    public Guid StudentId { get; private set; }
    public string ReceiptNumber { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? ExternalReference { get; private set; }
    public string CollectedBy { get; private set; }
    public DateTimeOffset CollectedAtUtc { get; private set; }
    public ReceiptStatus Status { get; private set; }
    public string? ReversalReason { get; private set; }
    public string? ReversedBy { get; private set; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }

    public static FeeReceipt Create(
        Guid tenantId,
        Guid schoolId,
        Guid campusId,
        Guid challanId,
        Guid studentId,
        string receiptNumber,
        DateOnly paymentDate,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string? externalReference,
        string collectedBy,
        DateTimeOffset collectedAtUtc)
        => new(tenantId, schoolId, campusId, challanId, studentId, receiptNumber, paymentDate, amount, currencyCode, method, externalReference, collectedBy, collectedAtUtc);

    public void Reverse(string reason, string actor, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (Status == ReceiptStatus.Reversed)
        {
            throw new InvalidOperationException("Receipt has already been reversed.");
        }

        Status = ReceiptStatus.Reversed;
        ReversalReason = reason.Trim();
        ReversedBy = actor.Trim();
        ReversedAtUtc = utcNow;
    }
}

/// <summary>
/// Outbox-ready accounting posting boundary (<c>fees.fee_posting_outbox</c>) decoupled from future General Ledger implementation (DEC-005).
/// </summary>
public sealed class FeePostingOutboxEntry : Entity<Guid>, ICampusScopedEntity
{
    private FeePostingOutboxEntry()
    {
        EventType = string.Empty;
        CurrencyCode = "PKR";
        PayloadJson = "{}";
    }

    private FeePostingOutboxEntry(
        Guid tenantId,
        Guid campusId,
        string eventType,
        Guid referenceId,
        decimal amount,
        string currencyCode,
        string payloadJson,
        DateTimeOffset occurredAtUtc) : this()
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        CampusId = campusId;
        EventType = eventType;
        ReferenceId = referenceId;
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        CurrencyCode = currencyCode;
        PayloadJson = payloadJson;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public string EventType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static FeePostingOutboxEntry Create(
        Guid tenantId,
        Guid campusId,
        string eventType,
        Guid referenceId,
        decimal amount,
        string currencyCode,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
        => new(tenantId, campusId, eventType, referenceId, amount, currencyCode, payloadJson, occurredAtUtc);
}
