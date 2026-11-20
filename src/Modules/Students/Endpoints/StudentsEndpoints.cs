using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Students.Application;
using SchoolOS.Modules.Students.Domain;
using SchoolOS.Modules.Students.Persistence;

namespace SchoolOS.Modules.Students.Endpoints;

public sealed record CreateHouseholdBody(string HouseholdCode, string DisplayName, Guid? PrimaryGuardianId = null);
public sealed record AddHouseholdMemberBody(Guid? StudentId, Guid? GuardianId, string? RoleLabel = null, bool IsBillingMember = false);

public sealed class StudentsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/students";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(ListCanonicalStudents, "")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.View);
        groupBuilder.MapNamedPost(RegisterStudent, "")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Create);
        groupBuilder.MapNamedGet(GetStudentDetail, "{id:guid}")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.View);
        groupBuilder.MapNamedPut(UpdateStudent, "{id:guid}")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);
        groupBuilder.MapNamedPost(ChangeStudentStatus, "{id:guid}/status")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);

        groupBuilder.MapNamedGet(ListStudentEnrollments, "{id:guid}/enrollments")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.View);
        groupBuilder.MapNamedPost(EnrollStudent, "{id:guid}/enrollments")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Create);
        groupBuilder.MapNamedPost(TransferStudent, "{id:guid}/transfer")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);

        groupBuilder.MapNamedGet(ListGuardians, "guardians")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateGuardian, "guardians")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Create);
        groupBuilder.MapNamedPost(LinkStudentGuardian, "{id:guid}/guardians")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);
        groupBuilder.MapNamedDelete(UnlinkStudentGuardian, "{id:guid}/guardians/{guardianId:guid}")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);

        groupBuilder.MapNamedGet(ListHouseholds, "households")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateHousehold, "households")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Create);
        groupBuilder.MapNamedPost(AddHouseholdMember, "households/{householdId:guid}/members")
            .RequireModulePermission(ModuleCode.Students, PermissionAction.Edit);
    }

    private static async Task<IResult> ListCanonicalStudents(
        StudentsDbContext db,
        ITenantContext tenantContext,
        string? search,
        StudentLifecycleStatus? status,
        Guid? campusId,
        Guid? academicYearId,
        Guid? gradeLevelId,
        CancellationToken ct)
    {
        var query = db.Students.AsNoTracking();

        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(s => s.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(s => s.CampusId == campusId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.AdmissionNumber.ToLower().Contains(term) ||
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term));
        }

        if (academicYearId.HasValue || gradeLevelId.HasValue)
        {
            var enrollQuery = db.Enrollments.AsNoTracking().Where(e => e.Status == EnrollmentStatus.Active);
            if (academicYearId.HasValue) enrollQuery = enrollQuery.Where(e => e.AcademicYearId == academicYearId.Value);
            if (gradeLevelId.HasValue) enrollQuery = enrollQuery.Where(e => e.GradeLevelId == gradeLevelId.Value);
            var studentIds = await enrollQuery.Select(e => e.StudentId).Distinct().ToListAsync(ct);
            query = query.Where(s => studentIds.Contains(s.Id));
        }

        var students = await query
            .OrderBy(s => s.AdmissionNumber)
            .Select(s => new
            {
                s.Id,
                s.SchoolId,
                s.CampusId,
                s.AdmissionNumber,
                s.FirstName,
                s.MiddleName,
                s.LastName,
                s.DateOfBirth,
                s.Gender,
                s.AdmissionDate,
                Status = s.Status.ToString(),
                s.SourceEnquiryId
            })
            .ToListAsync(ct);

        return Results.Ok(students);
    }

    private static async Task<IResult> RegisterStudent(
        RegisterStudentCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var (student, enrollment) = await service.RegisterStudentAsync(request, ct);
            return Results.Created($"/api/v1/students/{student.Id}", new
            {
                student.Id,
                student.SchoolId,
                student.CampusId,
                student.AdmissionNumber,
                student.FirstName,
                student.MiddleName,
                student.LastName,
                student.DateOfBirth,
                student.Gender,
                student.AdmissionDate,
                Status = student.Status.ToString(),
                InitialEnrollmentId = enrollment?.Id
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> GetStudentDetail(
        Guid id,
        StudentsDbContext db,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var student = await service.LoadAuthorizedStudentAsync(id, ct);

            var guardians = await (
                from sg in db.StudentGuardians.AsNoTracking()
                join g in db.Guardians.AsNoTracking() on sg.GuardianId equals g.Id
                where sg.StudentId == student.Id && sg.IsActive
                orderby sg.IsPrimaryContact descending, g.FirstName
                select new
                {
                    sg.Id,
                    sg.GuardianId,
                    g.FirstName,
                    g.LastName,
                    g.PrimaryPhone,
                    g.SecondaryPhone,
                    g.Email,
                    g.Occupation,
                    g.PreferredChannel,
                    RelationshipType = sg.RelationshipType.ToString(),
                    sg.IsPrimaryContact,
                    sg.IsEmergencyContact,
                    sg.HasLegalCustody,
                    sg.CanCollectStudent,
                    sg.ReceivesAcademicNotices,
                    sg.ReceivesFinancialNotices
                }).ToListAsync(ct);

            var households = await (
                from hm in db.HouseholdMembers.AsNoTracking()
                join h in db.Households.AsNoTracking() on hm.HouseholdId equals h.Id
                where hm.StudentId == student.Id && hm.IsActive
                select new
                {
                    h.Id,
                    h.HouseholdCode,
                    h.DisplayName,
                    hm.RoleLabel
                }).ToListAsync(ct);

            var enrollments = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentId == student.Id)
                .OrderByDescending(e => e.StartDate)
                .ThenByDescending(e => e.CreatedAtUtc)
                .Select(e => new
                {
                    e.Id,
                    e.SchoolId,
                    e.CampusId,
                    e.AcademicYearId,
                    e.GradeLevelId,
                    e.SectionId,
                    e.RollNumber,
                    e.StartDate,
                    e.EndDate,
                    Status = e.Status.ToString(),
                    e.PreviousEnrollmentId,
                    e.Reason
                })
                .ToListAsync(ct);

            var history = student.StatusHistory
                .OrderByDescending(h => h.ChangedAtUtc)
                .Select(h => new
                {
                    h.Id,
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    h.EffectiveDate,
                    h.Reason,
                    h.ChangedBy,
                    h.ChangedAtUtc
                })
                .ToList();

            return Results.Ok(new
            {
                student.Id,
                student.SchoolId,
                student.CampusId,
                student.AdmissionNumber,
                student.FirstName,
                student.MiddleName,
                student.LastName,
                student.DateOfBirth,
                student.Gender,
                student.AdmissionDate,
                Status = student.Status.ToString(),
                student.SourceEnquiryId,
                student.Notes,
                Guardians = guardians,
                Households = households,
                Enrollments = enrollments,
                StatusHistory = history
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> UpdateStudent(
        Guid id,
        UpdateStudentCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var student = await service.UpdateStudentAsync(id, request, ct);
            return Results.Ok(new
            {
                student.Id,
                student.AdmissionNumber,
                student.FirstName,
                student.MiddleName,
                student.LastName,
                student.DateOfBirth,
                student.Gender,
                student.Notes,
                Status = student.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> ChangeStudentStatus(
        Guid id,
        ChangeStudentStatusCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var (student, entry) = await service.ChangeStudentStatusAsync(id, request, ct);
            return Results.Ok(new
            {
                student.Id,
                Status = student.Status.ToString(),
                HistoryEntryId = entry.Id,
                FromStatus = entry.FromStatus.ToString(),
                ToStatus = entry.ToStatus.ToString(),
                entry.EffectiveDate,
                entry.Reason
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "INVALID_TRANSITION", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> ListStudentEnrollments(
        Guid id,
        StudentsDbContext db,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var student = await service.LoadAuthorizedStudentAsync(id, ct);
            var enrollments = await db.Enrollments.AsNoTracking()
                .Where(e => e.StudentId == student.Id)
                .OrderByDescending(e => e.StartDate)
                .ThenByDescending(e => e.CreatedAtUtc)
                .Select(e => new
                {
                    e.Id,
                    e.SchoolId,
                    e.CampusId,
                    e.AcademicYearId,
                    e.GradeLevelId,
                    e.SectionId,
                    e.RollNumber,
                    e.StartDate,
                    e.EndDate,
                    Status = e.Status.ToString(),
                    e.PreviousEnrollmentId,
                    e.Reason
                })
                .ToListAsync(ct);

            return Results.Ok(enrollments);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> EnrollStudent(
        Guid id,
        EnrollStudentCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var enrollment = await service.EnrollStudentAsync(id, request, ct);
            return Results.Created($"/api/v1/students/{id}/enrollments/{enrollment.Id}", new
            {
                enrollment.Id,
                enrollment.StudentId,
                enrollment.CampusId,
                enrollment.AcademicYearId,
                enrollment.GradeLevelId,
                enrollment.SectionId,
                enrollment.RollNumber,
                enrollment.StartDate,
                Status = enrollment.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "ENROLLMENT_CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> TransferStudent(
        Guid id,
        TransferStudentCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var (previous, current) = await service.TransferStudentAsync(id, request, ct);
            return Results.Ok(new
            {
                PreviousEnrollmentId = previous.Id,
                PreviousStatus = previous.Status.ToString(),
                NewEnrollmentId = current.Id,
                NewCampusId = current.CampusId,
                NewGradeLevelId = current.GradeLevelId,
                NewSectionId = current.SectionId,
                Status = current.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "TRANSFER_CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> ListGuardians(
        StudentsDbContext db,
        string? search,
        CancellationToken ct)
    {
        var query = db.Guardians.AsNoTracking().Where(g => g.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(term) ||
                g.LastName.ToLower().Contains(term) ||
                g.PrimaryPhone.Contains(term));
        }

        var items = await query
            .OrderBy(g => g.FirstName)
            .ThenBy(g => g.LastName)
            .Select(g => new
            {
                g.Id,
                g.FirstName,
                g.LastName,
                g.PrimaryPhone,
                g.SecondaryPhone,
                g.Email,
                g.Occupation,
                g.PreferredChannel
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateGuardian(
        CreateGuardianCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var g = await service.CreateGuardianAsync(request, ct);
            return Results.Created($"/api/v1/students/guardians/{g.Id}", new
            {
                g.Id,
                g.FirstName,
                g.LastName,
                g.PrimaryPhone,
                g.SecondaryPhone,
                g.Email,
                g.Occupation,
                g.PreferredChannel
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> LinkStudentGuardian(
        Guid id,
        LinkGuardianCommand request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var link = await service.LinkGuardianAsync(id, request, ct);
            return Results.Created($"/api/v1/students/{id}/guardians/{link.GuardianId}", new
            {
                link.Id,
                link.StudentId,
                link.GuardianId,
                RelationshipType = link.RelationshipType.ToString(),
                link.IsPrimaryContact,
                link.IsEmergencyContact,
                link.HasLegalCustody,
                link.CanCollectStudent,
                link.ReceivesAcademicNotices,
                link.ReceivesFinancialNotices
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> UnlinkStudentGuardian(
        Guid id,
        Guid guardianId,
        StudentsDbContext db,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            await service.LoadAuthorizedStudentAsync(id, ct);
            var link = await db.StudentGuardians
                .FirstOrDefaultAsync(sg => sg.StudentId == id && sg.GuardianId == guardianId && sg.IsActive, ct);
            if (link is null)
            {
                return Results.NotFound();
            }

            link.Deactivate();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ListHouseholds(StudentsDbContext db, CancellationToken ct)
    {
        var households = await db.Households.AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.HouseholdCode)
            .Select(h => new
            {
                h.Id,
                h.HouseholdCode,
                h.DisplayName,
                h.PrimaryGuardianId,
                StudentCount = db.HouseholdMembers.Count(m => m.HouseholdId == h.Id && m.StudentId != null && m.IsActive),
                GuardianCount = db.HouseholdMembers.Count(m => m.HouseholdId == h.Id && m.GuardianId != null && m.IsActive)
            })
            .ToListAsync(ct);

        return Results.Ok(households);
    }

    private static async Task<IResult> CreateHousehold(
        CreateHouseholdBody request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var h = await service.CreateHouseholdAsync(request.HouseholdCode, request.DisplayName, request.PrimaryGuardianId, ct);
            return Results.Created($"/api/v1/students/households/{h.Id}", new
            {
                h.Id,
                h.HouseholdCode,
                h.DisplayName,
                h.PrimaryGuardianId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }

    private static async Task<IResult> AddHouseholdMember(
        Guid householdId,
        AddHouseholdMemberBody request,
        StudentLifecycleService service,
        CancellationToken ct)
    {
        try
        {
            var member = await service.AddHouseholdMemberAsync(
                householdId,
                request.StudentId,
                request.GuardianId,
                request.RoleLabel ?? (request.StudentId.HasValue ? "Student" : "Guardian"),
                request.IsBillingMember,
                ct);

            return Results.Created($"/api/v1/students/households/{householdId}/members/{member.Id}", new
            {
                member.Id,
                member.HouseholdId,
                member.StudentId,
                member.GuardianId,
                member.RoleLabel,
                member.IsBillingMember
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
        }
    }
}
