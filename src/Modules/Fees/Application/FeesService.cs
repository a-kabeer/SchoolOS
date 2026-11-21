using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Fees.Domain;
using SchoolOS.Modules.Fees.Persistence;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Students.Contracts;

namespace SchoolOS.Modules.Fees.Application;

public sealed record CreateFeeHeadCommand(Guid SchoolId, string Code, string Name, FeeFrequency Frequency, bool IsOptional = false);

public sealed record CreateFeeScheduleCommand(
    Guid CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    Guid FeeHeadId,
    decimal Amount,
    string CurrencyCode = "PKR",
    decimal SiblingDiscountPercent = 0m,
    decimal LateFeePerDay = 0m);

public sealed record GenerateMonthlyChallansCommand(
    Guid CampusId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    string BillingPeriod,
    DateOnly IssueDate,
    DateOnly DueDate);

public sealed record GenerateChallansResultDto(
    int GeneratedCount,
    int SkippedExistingCount,
    decimal TotalNetAmount,
    IReadOnlyList<Guid> CreatedChallanIds);

public sealed record PostFeeReceiptCommand(
    Guid ChallanId,
    decimal Amount,
    DateOnly PaymentDate,
    PaymentMethod Method,
    string? ExternalReference = null);

public sealed record ReverseFeeReceiptCommand(string Reason);
public sealed record VoidFeeChallanCommand(string Reason);

public sealed record DefaulterChallanItemDto(
    Guid ChallanId,
    Guid StudentId,
    string ChallanNumber,
    string BillingPeriod,
    DateOnly DueDate,
    int DaysOverdue,
    string AgingBucket,
    decimal BalanceAmount,
    string CurrencyCode);

public sealed record DefaulterAgingSummaryDto(
    DateOnly AsOfDate,
    int OutstandingChallanCount,
    decimal TotalOutstanding,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61PlusAmount,
    IReadOnlyList<DefaulterChallanItemDto> Items);

public sealed class FeesService
{
    private readonly FeesDbContext _db;
    private readonly IOrganizationAcademicReader _organization;
    private readonly IStudentBillingReader _billingReader;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public FeesService(
        FeesDbContext db,
        IOrganizationAcademicReader organization,
        IStudentBillingReader billingReader,
        ITenantContext tenantContext,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _organization = organization;
        _billingReader = billingReader;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<FeeHead> CreateFeeHeadAsync(CreateFeeHeadCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var normalized = command.Code.Trim().ToUpperInvariant();

        var exists = await _db.FeeHeads.AnyAsync(
            h => h.SchoolId == command.SchoolId && h.Code == normalized,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Fee head '{normalized}' already exists for this school.");
        }

        var head = FeeHead.Create(tenantId, command.SchoolId, command.Code, command.Name, command.Frequency, command.IsOptional);
        _db.FeeHeads.Add(head);
        await _db.SaveChangesAsync(cancellationToken);
        return head;
    }

    public async Task<FeeSchedule> CreateFeeScheduleAsync(CreateFeeScheduleCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        EnsureCampusAuthorized(command.CampusId);

        var placement = await _organization.ValidatePlacementAsync(
            tenantId,
            command.CampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            sectionId: null,
            cancellationToken);

        if (!placement.IsValid || placement.Campus is null)
        {
            throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid campus, academic year, or grade level.");
        }

        var head = await _db.FeeHeads.FirstOrDefaultAsync(
            h => h.Id == command.FeeHeadId && h.SchoolId == placement.Campus.SchoolId && h.IsActive,
            cancellationToken)
            ?? throw new KeyNotFoundException("Fee head not found for this school.");

        var duplicate = await _db.FeeSchedules.AnyAsync(
            s => s.CampusId == command.CampusId
                && s.AcademicYearId == command.AcademicYearId
                && s.GradeLevelId == command.GradeLevelId
                && s.FeeHeadId == head.Id,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("A fee schedule already exists for this grade and fee head.");
        }

        var schedule = FeeSchedule.Create(
            tenantId,
            placement.Campus.SchoolId,
            command.CampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            head.Id,
            command.Amount,
            command.CurrencyCode,
            command.SiblingDiscountPercent,
            command.LateFeePerDay);

        _db.FeeSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task<GenerateChallansResultDto> GenerateMonthlyChallansAsync(
        GenerateMonthlyChallansCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        EnsureCampusAuthorized(command.CampusId);

        var placement = await _organization.ValidatePlacementAsync(
            tenantId,
            command.CampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            sectionId: null,
            cancellationToken);

        if (!placement.IsValid || placement.Campus is null)
        {
            throw new InvalidOperationException(placement.ErrorMessage ?? "Invalid campus, academic year, or grade level.");
        }

        var schedules = await (
            from s in _db.FeeSchedules.AsNoTracking()
            join h in _db.FeeHeads.AsNoTracking() on s.FeeHeadId equals h.Id
            where s.CampusId == command.CampusId
                && s.AcademicYearId == command.AcademicYearId
                && s.GradeLevelId == command.GradeLevelId
                && s.IsActive
                && h.IsActive
                && !h.IsOptional
            select new { Schedule = s, HeadName = h.Name }
        ).ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            throw new InvalidOperationException("No active mandatory fee schedules found for this campus, academic year, and grade level.");
        }

        var students = await _billingReader.GetBillableStudentsAsync(
            tenantId,
            command.CampusId,
            command.AcademicYearId,
            command.GradeLevelId,
            cancellationToken);

        if (students.Count == 0)
        {
            return new GenerateChallansResultDto(0, 0, 0m, []);
        }

        var studentIds = students.Select(s => s.StudentId).ToList();
        var existingStudentIds = (await _db.FeeChallans.AsNoTracking()
            .Where(c => c.AcademicYearId == command.AcademicYearId
                && c.BillingPeriod == command.BillingPeriod
                && c.Status != ChallanStatus.Voided
                && studentIds.Contains(c.StudentId))
            .Select(c => c.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var now = _timeProvider.GetUtcNow();
        var currency = schedules[0].Schedule.CurrencyCode;
        var createdIds = new List<Guid>();
        int skipped = 0;
        decimal totalNet = 0m;

        foreach (var student in students)
        {
            if (existingStudentIds.Contains(student.StudentId))
            {
                skipped++;
                continue;
            }

            var lines = schedules.Select(item =>
            {
                var discount = item.Schedule.CalculateDiscountForSiblingCount(student.ActiveHouseholdSiblingCount);
                return FeeChallanLine.Create(
                    tenantId,
                    item.Schedule.FeeHeadId,
                    item.HeadName,
                    item.Schedule.Amount,
                    discount);
            }).ToList();

            var challanNumber = $"CH-{command.BillingPeriod.Replace("-", "", StringComparison.Ordinal)}-{student.AdmissionNumber}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

            var challan = FeeChallan.Create(
                tenantId,
                placement.Campus.SchoolId,
                command.CampusId,
                student.StudentId,
                student.HouseholdId,
                command.AcademicYearId,
                command.GradeLevelId,
                command.BillingPeriod,
                challanNumber,
                currency,
                command.IssueDate,
                command.DueDate,
                lines);

            _db.FeeChallans.Add(challan);
            _db.FeePostingOutbox.Add(FeePostingOutboxEntry.Create(
                tenantId,
                command.CampusId,
                "ChallanIssued",
                challan.Id,
                challan.NetAmount,
                challan.CurrencyCode,
                JsonSerializer.Serialize(new { challan.ChallanNumber, challan.StudentId, challan.BillingPeriod, challan.NetAmount }),
                now));

            createdIds.Add(challan.Id);
            totalNet += challan.NetAmount;
        }

        if (createdIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new GenerateChallansResultDto(
            createdIds.Count,
            skipped,
            Math.Round(totalNet, 2, MidpointRounding.AwayFromZero),
            createdIds);
    }

    public async Task<FeeReceipt> PostReceiptAsync(PostFeeReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var challan = await _db.FeeChallans.FirstOrDefaultAsync(c => c.Id == command.ChallanId, cancellationToken)
            ?? throw new KeyNotFoundException("Fee challan not found.");

        EnsureCampusAuthorized(challan.CampusId);

        challan.ApplyPayment(command.Amount);

        var now = _timeProvider.GetUtcNow();
        var actor = _tenantContext.UserId ?? "system";
        var receiptNumber = $"RCP-{command.PaymentDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var receipt = FeeReceipt.Create(
            tenantId,
            challan.SchoolId,
            challan.CampusId,
            challan.Id,
            challan.StudentId,
            receiptNumber,
            command.PaymentDate,
            command.Amount,
            challan.CurrencyCode,
            command.Method,
            command.ExternalReference,
            actor,
            now);

        _db.FeeReceipts.Add(receipt);
        _db.FeePostingOutbox.Add(FeePostingOutboxEntry.Create(
            tenantId,
            challan.CampusId,
            "ReceiptPosted",
            receipt.Id,
            receipt.Amount,
            receipt.CurrencyCode,
            JsonSerializer.Serialize(new { receipt.ReceiptNumber, receipt.ChallanId, receipt.StudentId, Method = receipt.Method.ToString(), receipt.Amount }),
            now));

        await _db.SaveChangesAsync(cancellationToken);
        return receipt;
    }

    public async Task<FeeReceipt> ReverseReceiptAsync(Guid receiptId, ReverseFeeReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var receipt = await _db.FeeReceipts.FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken)
            ?? throw new KeyNotFoundException("Fee receipt not found.");

        EnsureCampusAuthorized(receipt.CampusId);

        var challan = await _db.FeeChallans.FirstOrDefaultAsync(c => c.Id == receipt.ChallanId, cancellationToken)
            ?? throw new KeyNotFoundException("Parent fee challan not found.");

        var now = _timeProvider.GetUtcNow();
        var actor = _tenantContext.UserId ?? "system";

        receipt.Reverse(command.Reason, actor, now);
        challan.ReversePayment(receipt.Amount);

        _db.FeePostingOutbox.Add(FeePostingOutboxEntry.Create(
            tenantId,
            receipt.CampusId,
            "ReceiptReversed",
            receipt.Id,
            -receipt.Amount,
            receipt.CurrencyCode,
            JsonSerializer.Serialize(new { receipt.ReceiptNumber, receipt.ChallanId, receipt.ReversalReason, ReversedAmount = receipt.Amount }),
            now));

        await _db.SaveChangesAsync(cancellationToken);
        return receipt;
    }

    public async Task<FeeChallan> VoidChallanAsync(Guid challanId, VoidFeeChallanCommand command, CancellationToken cancellationToken = default)
    {
        _ = RequireTenantId();
        var challan = await _db.FeeChallans.FirstOrDefaultAsync(c => c.Id == challanId, cancellationToken)
            ?? throw new KeyNotFoundException("Fee challan not found.");

        EnsureCampusAuthorized(challan.CampusId);

        challan.Void(command.Reason);
        await _db.SaveChangesAsync(cancellationToken);
        return challan;
    }

    public async Task<DefaulterAgingSummaryDto> GetDefaulterAgingAsync(
        Guid? campusId,
        Guid? academicYearId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        _ = RequireTenantId();

        var query = _db.FeeChallans.AsNoTracking()
            .Where(c => c.Status != ChallanStatus.Voided && c.Status != ChallanStatus.Paid && c.BalanceAmount > 0m);

        if (_tenantContext.CampusId.HasValue)
        {
            query = query.Where(c => c.CampusId == _tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(c => c.CampusId == campusId.Value);
        }

        if (academicYearId.HasValue)
        {
            query = query.Where(c => c.AcademicYearId == academicYearId.Value);
        }

        var openChallans = await query.OrderBy(c => c.DueDate).ToListAsync(cancellationToken);
        return ComputeAging(openChallans, asOfDate);
    }

    public static DefaulterAgingSummaryDto ComputeAging(IReadOnlyList<FeeChallan> openChallans, DateOnly asOfDate)
    {
        decimal current = 0m, d1To30 = 0m, d31To60 = 0m, d61Plus = 0m;
        var items = new List<DefaulterChallanItemDto>(openChallans.Count);

        foreach (var c in openChallans)
        {
            var daysOverdue = Math.Max(0, asOfDate.DayNumber - c.DueDate.DayNumber);
            string bucket;
            if (daysOverdue == 0)
            {
                bucket = "Current";
                current += c.BalanceAmount;
            }
            else if (daysOverdue <= 30)
            {
                bucket = "1-30 Days";
                d1To30 += c.BalanceAmount;
            }
            else if (daysOverdue <= 60)
            {
                bucket = "31-60 Days";
                d31To60 += c.BalanceAmount;
            }
            else
            {
                bucket = "61+ Days";
                d61Plus += c.BalanceAmount;
            }

            items.Add(new DefaulterChallanItemDto(
                c.Id,
                c.StudentId,
                c.ChallanNumber,
                c.BillingPeriod,
                c.DueDate,
                daysOverdue,
                bucket,
                c.BalanceAmount,
                c.CurrencyCode));
        }

        var total = Math.Round(current + d1To30 + d31To60 + d61Plus, 2, MidpointRounding.AwayFromZero);
        return new DefaulterAgingSummaryDto(
            asOfDate,
            items.Count,
            total,
            Math.Round(current, 2, MidpointRounding.AwayFromZero),
            Math.Round(d1To30, 2, MidpointRounding.AwayFromZero),
            Math.Round(d31To60, 2, MidpointRounding.AwayFromZero),
            Math.Round(d61Plus, 2, MidpointRounding.AwayFromZero),
            items);
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.TenantId.HasValue || _tenantContext.TenantId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        return _tenantContext.TenantId.Value;
    }

    private void EnsureCampusAuthorized(Guid targetCampusId)
    {
        if (_tenantContext.CampusId.HasValue && _tenantContext.CampusId.Value != targetCampusId)
        {
            throw new UnauthorizedAccessException("Caller is not authorized to access another campus.");
        }
    }
}
