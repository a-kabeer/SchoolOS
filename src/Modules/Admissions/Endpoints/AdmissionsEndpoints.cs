using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Admissions.Domain;
using SchoolOS.Modules.Admissions.Persistence;

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
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            return Results.Unauthorized();
        }

        var campusId = tenantContext.CampusId ?? request.CampusId;
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
        CancellationToken cancellationToken)
    {
        var enquiry = await db.AdmissionEnquiries
            .Include(e => e.FollowUps)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
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
        CancellationToken cancellationToken)
    {
        var enquiry = await db.AdmissionEnquiries
            .Include(e => e.Visits)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
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
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var enquiry = await db.AdmissionEnquiries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (enquiry is null)
        {
            return Results.NotFound();
        }

        var admissionDate = request.AdmissionDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var student = enquiry.ConvertToStudent(
            request.AdmissionNumber,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            admissionDate);

        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/admissions/students/{student.Id}",
            new { student.Id, student.AdmissionNumber, student.FirstName, student.LastName, EnquiryId = enquiry.Id, EnquiryStatus = enquiry.Status });
    }

    private static async Task<IResult> ListStudents(
        AdmissionsDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var query = db.StudentProfiles.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(s => s.CampusId == tenantContext.CampusId.Value);
        }

        var items = await query
            .OrderBy(s => s.AdmissionNumber)
            .Select(s => new
            {
                s.Id,
                s.CampusId,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                s.TargetGrade,
                s.Status,
                s.SourceEnquiryId
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetStudentById(
        Guid id,
        AdmissionsDbContext db,
        CancellationToken cancellationToken)
    {
        var student = await db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return Results.NotFound();
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
