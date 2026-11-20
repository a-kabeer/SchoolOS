using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Admissions.Domain;
using SchoolOS.Modules.Admissions.Persistence;
using SchoolOS.Modules.Organization.Contracts;
using SchoolOS.Modules.Students.Contracts;

namespace SchoolOS.Modules.Admissions.Endpoints;

public sealed record CreateEnquiryBody(
    Guid CampusId,
    string ApplicantName,
    string GuardianName,
    string GuardianPhone,
    string? GuardianEmail,
    string TargetGrade,
    string AcademicYear,
    string? Notes);

public sealed record AddFollowUpBody(DateTimeOffset FollowUpAtUtc, FollowUpChannel Channel, string Notes);
public sealed record ScheduleVisitBody(DateTimeOffset ScheduledAtUtc, string VisitorName, string? Notes);
public sealed record ConvertEnquiryBody(
    string AdmissionNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    DateOnly? AdmissionDate = null);

public sealed class AdmissionsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/admissions";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(ListEnquiries, "enquiries")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateEnquiry, "enquiries")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.Create);
        groupBuilder.MapNamedPost(AddEnquiryFollowUp, "enquiries/{id:guid}/follow-ups")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.Edit);
        groupBuilder.MapNamedPost(ScheduleCampusVisit, "enquiries/{id:guid}/visits")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.Edit);
        groupBuilder.MapNamedPost(ConvertEnquiryToStudent, "enquiries/{id:guid}/convert")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListStudents, "students")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.View);
        groupBuilder.MapNamedGet(GetStudentById, "students/{id:guid}")
            .RequireModulePermission(ModuleCode.Admissions, PermissionAction.View);
    }

    private static async Task<IResult> ListEnquiries(
        AdmissionsDbContext db,
        ITenantContext tenantContext,
        AdmissionEnquiryStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.AdmissionEnquiries.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(e => e.CampusId == tenantContext.CampusId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new
            {
                e.Id,
                e.CampusId,
                e.ApplicantName,
                e.GuardianName,
                e.GuardianPhone,
                e.TargetGrade,
                e.AcademicYear,
                e.Status,
                e.ConvertedStudentProfileId
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateEnquiry(
        CreateEnquiryBody request,
        AdmissionsDbContext db,
        IOrganizationAcademicReader organization,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (tenantContext.CampusId.HasValue && request.CampusId != Guid.Empty && request.CampusId != tenantContext.CampusId.Value)
        {
            return Results.Forbid();
        }

        var campusId = tenantContext.CampusId ?? request.CampusId;
        var campus = await organization.GetCampusAsync(tenantContext.TenantId.Value, campusId, cancellationToken);
        if (campus is null || !campus.IsActive)
        {
            return Results.BadRequest(new { code = "CAMPUS_NOT_FOUND", message = "Target campus was not found or is inactive in the authenticated tenant." });
        }

        var enquiry = AdmissionEnquiry.Create(
            tenantContext.TenantId.Value,
            campusId,
            request.ApplicantName,
            request.GuardianName,
            request.GuardianPhone,
            request.GuardianEmail,
            request.TargetGrade,
            request.AcademicYear,
            request.Notes);

        db.AdmissionEnquiries.Add(enquiry);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/admissions/enquiries/{enquiry.Id}",
            new { enquiry.Id, enquiry.CampusId, enquiry.ApplicantName, enquiry.Status });
    }

    private static async Task<IResult> AddEnquiryFollowUp(
        Guid id,
        AddFollowUpBody request,
        AdmissionsDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var enquiry = await db.AdmissionEnquiries
            .Include(e => e.FollowUps)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
        }

        if (tenantContext.CampusId.HasValue && enquiry.CampusId != tenantContext.CampusId.Value)
        {
            return Results.Forbid();
        }

        var followUp = enquiry.AddFollowUp(request.FollowUpAtUtc, request.Channel, request.Notes);
        db.EnquiryFollowUps.Add(followUp);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/admissions/enquiries/{id}/follow-ups/{followUp.Id}",
            new { followUp.Id, enquiryStatus = enquiry.Status });
    }

    private static async Task<IResult> ScheduleCampusVisit(
        Guid id,
        ScheduleVisitBody request,
        AdmissionsDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var enquiry = await db.AdmissionEnquiries
            .Include(e => e.Visits)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
        }

        if (tenantContext.CampusId.HasValue && enquiry.CampusId != tenantContext.CampusId.Value)
        {
            return Results.Forbid();
        }

        var visit = enquiry.ScheduleVisit(request.ScheduledAtUtc, request.VisitorName, request.Notes);
        db.CampusVisits.Add(visit);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/admissions/enquiries/{id}/visits/{visit.Id}",
            new { visit.Id, enquiryStatus = enquiry.Status });
    }

    private static async Task<IResult> ConvertEnquiryToStudent(
        Guid id,
        ConvertEnquiryBody request,
        AdmissionsDbContext db,
        IStudentAdmissionBridge studentBridge,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        var enquiry = await db.AdmissionEnquiries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
        }

        if (tenantContext.CampusId.HasValue && enquiry.CampusId != tenantContext.CampusId.Value)
        {
            return Results.Forbid();
        }

        var admissionDate = request.AdmissionDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var admitted = await studentBridge.AdmitFromEnquiryAsync(
            new AdmitStudentFromEnquiryCommand(
                tenantContext.TenantId.Value,
                enquiry.CampusId,
                enquiry.Id,
                request.AdmissionNumber,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                admissionDate,
                enquiry.GuardianName,
                enquiry.GuardianPhone,
                enquiry.GuardianEmail,
                enquiry.TargetGrade),
            cancellationToken);

        if (enquiry.Status != AdmissionEnquiryStatus.Admitted || enquiry.ConvertedStudentProfileId != admitted.StudentId)
        {
            enquiry.MarkAdmitted(admitted.StudentId);
            await db.SaveChangesAsync(cancellationToken);
        }

        var payload = new
        {
            Id = admitted.StudentId,
            admitted.AdmissionNumber,
            admitted.FirstName,
            admitted.LastName,
            EnquiryId = enquiry.Id,
            EnquiryStatus = enquiry.Status,
            admitted.WasExisting
        };

        return admitted.WasExisting
            ? Results.Ok(payload)
            : Results.Created($"/api/v1/students/{admitted.StudentId}", payload);
    }

    private static async Task<IResult> ListStudents(
        AdmissionsDbContext db,
        IStudentAdmissionBridge studentBridge,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var canonical = await studentBridge.ListAdmittedStudentsAsync(tenantContext.CampusId, cancellationToken);
        var seenIds = canonical.Select(s => s.Id).ToHashSet();

        var legacyQuery = db.StudentProfiles.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            legacyQuery = legacyQuery.Where(s => s.CampusId == tenantContext.CampusId.Value);
        }

        var legacy = await legacyQuery
            .OrderBy(s => s.AdmissionNumber)
            .Select(s => new
            {
                s.Id,
                s.CampusId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                s.TargetGrade,
                Status = s.Status.ToString(),
                s.SourceEnquiryId
            })
            .ToListAsync(cancellationToken);

        var combined = canonical
            .Select(s => new
            {
                s.Id,
                s.CampusId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                TargetGrade = string.Empty,
                s.Status,
                s.SourceEnquiryId
            })
            .Concat(legacy.Where(l => !seenIds.Contains(l.Id)))
            .OrderBy(s => s.AdmissionNumber)
            .ToList();

        return Results.Ok(combined);
    }

    private static async Task<IResult> GetStudentById(
        Guid id,
        AdmissionsDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var student = await db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return Results.NotFound();
        }

        if (tenantContext.CampusId.HasValue && student.CampusId != tenantContext.CampusId.Value)
        {
            return Results.Forbid();
        }

        return Results.Ok(new
        {
            student.Id,
            student.CampusId,
            student.AdmissionNumber,
            student.FirstName,
            student.LastName,
            student.DateOfBirth,
            student.GuardianName,
            student.GuardianPhone,
            student.GuardianEmail,
            student.TargetGrade,
            student.Status,
            student.SourceEnquiryId
        });
    }
}
