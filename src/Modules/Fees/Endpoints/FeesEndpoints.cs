using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Fees.Application;
using SchoolOS.Modules.Fees.Domain;
using SchoolOS.Modules.Fees.Persistence;

namespace SchoolOS.Modules.Fees.Endpoints;

public sealed class FeesEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/fees";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(ListFeeHeads, "heads")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateFeeHead, "heads")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListFeeSchedules, "schedules")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateFeeSchedule, "schedules")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListFeeChallans, "challans")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.View);
        groupBuilder.MapNamedPost(GenerateMonthlyChallans, "challans/generate")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Create);
        groupBuilder.MapNamedPost(VoidFeeChallan, "challans/{id:guid}/void")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Edit);

        groupBuilder.MapNamedGet(ListFeeReceipts, "receipts")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.View);
        groupBuilder.MapNamedPost(PostFeeReceipt, "receipts")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Create);
        groupBuilder.MapNamedPost(ReverseFeeReceipt, "receipts/{id:guid}/reverse")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.Edit);

        groupBuilder.MapNamedGet(GetDefaulterAging, "defaulters")
            .RequireModulePermission(ModuleCode.Fees, PermissionAction.View);
    }

    private static async Task<IResult> ListFeeHeads(FeesDbContext db, Guid? schoolId, CancellationToken ct)
    {
        var query = db.FeeHeads.AsNoTracking();
        if (schoolId.HasValue) query = query.Where(h => h.SchoolId == schoolId.Value);

        var items = await query
            .OrderBy(h => h.Code)
            .Select(h => new { h.Id, h.SchoolId, h.Code, h.Name, Frequency = h.Frequency.ToString(), h.IsOptional, h.IsActive })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateFeeHead(CreateFeeHeadCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var head = await service.CreateFeeHeadAsync(request, ct);
            return Results.Created(
                $"/api/v1/fees/heads/{head.Id}",
                new { head.Id, head.SchoolId, head.Code, head.Name, Frequency = head.Frequency.ToString(), head.IsOptional, head.IsActive });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_FEE_HEAD", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "FEE_HEAD_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> ListFeeSchedules(
        FeesDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        Guid? academicYearId,
        Guid? gradeLevelId,
        CancellationToken ct)
    {
        var query = from s in db.FeeSchedules.AsNoTracking()
                    join h in db.FeeHeads.AsNoTracking() on s.FeeHeadId equals h.Id
                    select new
                    {
                        s.Id,
                        s.SchoolId,
                        s.CampusId,
                        s.AcademicYearId,
                        s.GradeLevelId,
                        s.FeeHeadId,
                        FeeHeadCode = h.Code,
                        FeeHeadName = h.Name,
                        s.Amount,
                        s.CurrencyCode,
                        s.SiblingDiscountPercent,
                        s.LateFeePerDay,
                        s.IsActive
                    };

        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(x => x.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(x => x.CampusId == campusId.Value);
        }

        if (academicYearId.HasValue) query = query.Where(x => x.AcademicYearId == academicYearId.Value);
        if (gradeLevelId.HasValue) query = query.Where(x => x.GradeLevelId == gradeLevelId.Value);

        return Results.Ok(await query.OrderBy(x => x.FeeHeadCode).ToListAsync(ct));
    }

    private static async Task<IResult> CreateFeeSchedule(CreateFeeScheduleCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var schedule = await service.CreateFeeScheduleAsync(request, ct);
            return Results.Created(
                $"/api/v1/fees/schedules/{schedule.Id}",
                new
                {
                    schedule.Id,
                    schedule.SchoolId,
                    schedule.CampusId,
                    schedule.AcademicYearId,
                    schedule.GradeLevelId,
                    schedule.FeeHeadId,
                    schedule.Amount,
                    schedule.CurrencyCode,
                    schedule.SiblingDiscountPercent,
                    schedule.LateFeePerDay,
                    schedule.IsActive
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "FEE_HEAD_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_FEE_SCHEDULE", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "FEE_SCHEDULE_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> ListFeeChallans(
        FeesDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        Guid? studentId,
        string? billingPeriod,
        ChallanStatus? status,
        CancellationToken ct)
    {
        var query = db.FeeChallans.AsNoTracking().Include(c => c.Lines).AsQueryable();

        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(c => c.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(c => c.CampusId == campusId.Value);
        }

        if (studentId.HasValue) query = query.Where(c => c.StudentId == studentId.Value);
        if (!string.IsNullOrWhiteSpace(billingPeriod)) query = query.Where(c => c.BillingPeriod == billingPeriod.Trim());
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        var items = await query
            .OrderByDescending(c => c.IssueDate)
            .ThenBy(c => c.ChallanNumber)
            .Select(c => new
            {
                c.Id,
                c.CampusId,
                c.StudentId,
                c.HouseholdId,
                c.AcademicYearId,
                c.GradeLevelId,
                c.BillingPeriod,
                c.ChallanNumber,
                c.CurrencyCode,
                c.IssueDate,
                c.DueDate,
                c.GrossAmount,
                c.DiscountAmount,
                c.LateFeeAmount,
                c.NetAmount,
                c.PaidAmount,
                c.BalanceAmount,
                Status = c.Status.ToString(),
                c.VoidReason,
                Lines = c.Lines.Select(l => new { l.Id, l.FeeHeadId, l.Description, l.GrossAmount, l.DiscountAmount, l.NetAmount }).ToList()
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> GenerateMonthlyChallans(GenerateMonthlyChallansCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var result = await service.GenerateMonthlyChallansAsync(request, ct);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_CHALLAN_GENERATION", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CHALLAN_GENERATION_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> VoidFeeChallan(Guid id, VoidFeeChallanCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var challan = await service.VoidChallanAsync(id, request, ct);
            return Results.Ok(new { challan.Id, Status = challan.Status.ToString(), challan.VoidReason });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "CHALLAN_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_VOID_REASON", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CHALLAN_VOID_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> ListFeeReceipts(
        FeesDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        Guid? challanId,
        Guid? studentId,
        CancellationToken ct)
    {
        var query = db.FeeReceipts.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(r => r.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(r => r.CampusId == campusId.Value);
        }

        if (challanId.HasValue) query = query.Where(r => r.ChallanId == challanId.Value);
        if (studentId.HasValue) query = query.Where(r => r.StudentId == studentId.Value);

        var items = await query
            .OrderByDescending(r => r.CollectedAtUtc)
            .Take(200)
            .Select(r => new
            {
                r.Id,
                r.CampusId,
                r.ChallanId,
                r.StudentId,
                r.ReceiptNumber,
                r.PaymentDate,
                r.Amount,
                r.CurrencyCode,
                Method = r.Method.ToString(),
                r.ExternalReference,
                r.CollectedBy,
                r.CollectedAtUtc,
                Status = r.Status.ToString(),
                r.ReversalReason,
                r.ReversedBy,
                r.ReversedAtUtc
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> PostFeeReceipt(PostFeeReceiptCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var receipt = await service.PostReceiptAsync(request, ct);
            return Results.Created(
                $"/api/v1/fees/receipts/{receipt.Id}",
                new
                {
                    receipt.Id,
                    receipt.ChallanId,
                    receipt.StudentId,
                    receipt.ReceiptNumber,
                    receipt.PaymentDate,
                    receipt.Amount,
                    receipt.CurrencyCode,
                    Method = receipt.Method.ToString(),
                    receipt.ExternalReference,
                    Status = receipt.Status.ToString()
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "CHALLAN_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_PAYMENT", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "PAYMENT_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> ReverseFeeReceipt(Guid id, ReverseFeeReceiptCommand request, FeesService service, CancellationToken ct)
    {
        try
        {
            var receipt = await service.ReverseReceiptAsync(id, request, ct);
            return Results.Ok(new
            {
                receipt.Id,
                receipt.ReceiptNumber,
                Status = receipt.Status.ToString(),
                receipt.ReversalReason,
                receipt.ReversedBy,
                receipt.ReversedAtUtc
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "RECEIPT_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_REVERSAL_REASON", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "RECEIPT_REVERSAL_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> GetDefaulterAging(
        Guid? campusId,
        Guid? academicYearId,
        DateOnly? asOfDate,
        FeesService service,
        CancellationToken ct)
    {
        var targetDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await service.GetDefaulterAgingAsync(campusId, academicYearId, targetDate, ct);
        return Results.Ok(summary);
    }
}
