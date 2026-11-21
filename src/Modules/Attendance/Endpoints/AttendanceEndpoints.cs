using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Attendance.Application;
using SchoolOS.Modules.Attendance.Persistence;

namespace SchoolOS.Modules.Attendance.Endpoints;

public sealed class AttendanceEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/attendance";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        // Staff Directory & Teacher Section Assignments (Core ModuleCode.Staff)
        groupBuilder.MapNamedGet(ListStaffMembers, "staff")
            .RequireModulePermission(ModuleCode.Staff, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateStaffMember, "staff")
            .RequireModulePermission(ModuleCode.Staff, PermissionAction.Create);
        groupBuilder.MapNamedGet(ListTeacherAssignments, "staff/assignments")
            .RequireModulePermission(ModuleCode.Staff, PermissionAction.View);
        groupBuilder.MapNamedPost(AssignTeacherSection, "staff/{staffId:guid}/assignments")
            .RequireModulePermission(ModuleCode.Staff, PermissionAction.Edit);

        // Daily Attendance Registers, Corrections & Summaries (Paid ModuleCode.Attendance)
        groupBuilder.MapNamedGet(GetSectionRegister, "sections/{sectionId:guid}/register")
            .RequireModulePermission(ModuleCode.Attendance, PermissionAction.View);
        groupBuilder.MapNamedPost(MarkSectionAttendance, "sections/{sectionId:guid}/register")
            .RequireModulePermission(ModuleCode.Attendance, PermissionAction.Create);
        groupBuilder.MapNamedGet(GetSectionAttendanceSummary, "sections/{sectionId:guid}/summary")
            .RequireModulePermission(ModuleCode.Attendance, PermissionAction.View);
        groupBuilder.MapNamedGet(GetStudentAttendanceSummary, "students/{studentId:guid}/summary")
            .RequireModulePermission(ModuleCode.Attendance, PermissionAction.View);
        groupBuilder.MapNamedGet(ListAttendanceCorrections, "corrections")
            .RequireModulePermission(ModuleCode.Attendance, PermissionAction.View);
    }

    private static async Task<IResult> ListStaffMembers(
        AttendanceDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        CancellationToken ct)
    {
        var query = db.StaffMembers.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(s => s.CampusId == tenantContext.CampusId.Value);
        }
        else if (campusId.HasValue)
        {
            query = query.Where(s => s.CampusId == campusId.Value);
        }

        var items = await query
            .OrderBy(s => s.EmployeeCode)
            .Select(s => new
            {
                s.Id,
                s.SchoolId,
                s.CampusId,
                s.UserId,
                s.EmployeeCode,
                s.FirstName,
                s.LastName,
                s.Email,
                s.Phone,
                RoleType = s.RoleType.ToString(),
                s.IsActive
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateStaffMember(
        CreateStaffMemberCommand request,
        AttendanceService service,
        CancellationToken ct)
    {
        try
        {
            var staff = await service.CreateStaffMemberAsync(request, ct);
            return Results.Created(
                $"/api/v1/attendance/staff/{staff.Id}",
                new
                {
                    staff.Id,
                    staff.SchoolId,
                    staff.CampusId,
                    staff.UserId,
                    staff.EmployeeCode,
                    staff.FirstName,
                    staff.LastName,
                    staff.Email,
                    staff.Phone,
                    RoleType = staff.RoleType.ToString(),
                    staff.IsActive
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_STAFF_DATA", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "STAFF_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> ListTeacherAssignments(
        AttendanceDbContext db,
        ITenantContext tenantContext,
        Guid? academicYearId,
        Guid? sectionId,
        CancellationToken ct)
    {
        var query = from a in db.TeacherSectionAssignments.AsNoTracking()
                    join s in db.StaffMembers.AsNoTracking() on a.StaffMemberId equals s.Id
                    select new
                    {
                        a.Id,
                        a.CampusId,
                        a.StaffMemberId,
                        StaffName = s.FirstName + " " + s.LastName,
                        s.EmployeeCode,
                        a.AcademicYearId,
                        a.GradeLevelId,
                        a.SectionId,
                        a.IsHomeroomTeacher,
                        a.IsActive
                    };

        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(x => x.CampusId == tenantContext.CampusId.Value);
        }

        if (academicYearId.HasValue) query = query.Where(x => x.AcademicYearId == academicYearId.Value);
        if (sectionId.HasValue) query = query.Where(x => x.SectionId == sectionId.Value);

        return Results.Ok(await query.OrderBy(x => x.EmployeeCode).ToListAsync(ct));
    }

    private static async Task<IResult> AssignTeacherSection(
        Guid staffId,
        AssignTeacherSectionCommand request,
        AttendanceService service,
        CancellationToken ct)
    {
        try
        {
            var assignment = await service.AssignTeacherToSectionAsync(staffId, request, ct);
            return Results.Created(
                $"/api/v1/attendance/staff/{staffId}/assignments/{assignment.Id}",
                new
                {
                    assignment.Id,
                    assignment.StaffMemberId,
                    assignment.CampusId,
                    assignment.AcademicYearId,
                    assignment.GradeLevelId,
                    assignment.SectionId,
                    assignment.IsHomeroomTeacher,
                    assignment.IsActive
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "STAFF_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "ASSIGNMENT_INVALID", message = ex.Message });
        }
    }

    private static async Task<IResult> GetSectionRegister(
        Guid sectionId,
        DateOnly? date,
        HttpContext httpContext,
        AttendanceService service,
        CancellationToken ct)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            var register = await service.GetSectionRegisterAsync(sectionId, targetDate, httpContext.User, ct);
            return Results.Ok(register);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "SECTION_NOT_FOUND", message = ex.Message });
        }
    }

    private static async Task<IResult> MarkSectionAttendance(
        Guid sectionId,
        MarkSectionAttendanceCommand request,
        HttpContext httpContext,
        AttendanceService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.MarkSectionAttendanceAsync(sectionId, request, httpContext.User, ct);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "SECTION_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { code = "INVALID_ATTENDANCE_INPUT", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { code = "ATTENDANCE_CONFLICT", message = ex.Message });
        }
    }

    private static async Task<IResult> GetSectionAttendanceSummary(
        Guid sectionId,
        DateOnly? from,
        DateOnly? to,
        HttpContext httpContext,
        AttendanceService service,
        CancellationToken ct)
    {
        try
        {
            var summary = await service.GetSectionSummaryAsync(sectionId, from, to, httpContext.User, ct);
            return Results.Ok(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { code = "SECTION_NOT_FOUND", message = ex.Message });
        }
    }

    private static async Task<IResult> GetStudentAttendanceSummary(
        Guid studentId,
        Guid? academicYearId,
        DateOnly? from,
        DateOnly? to,
        AttendanceService service,
        CancellationToken ct)
    {
        try
        {
            var summary = await service.GetStudentSummaryAsync(studentId, academicYearId, from, to, ct);
            return Results.Ok(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> ListAttendanceCorrections(
        AttendanceDbContext db,
        ITenantContext tenantContext,
        Guid? studentId,
        DateOnly? date,
        CancellationToken ct)
    {
        var query = db.AttendanceCorrectionAudits.AsNoTracking();
        if (tenantContext.CampusId.HasValue)
        {
            query = query.Where(c => c.CampusId == tenantContext.CampusId.Value);
        }
        if (studentId.HasValue) query = query.Where(c => c.StudentId == studentId.Value);
        if (date.HasValue) query = query.Where(c => c.AttendanceDate == date.Value);

        var items = await query
            .OrderByDescending(c => c.CorrectedAtUtc)
            .Take(100)
            .Select(c => new
            {
                c.Id,
                c.EntryId,
                c.RegisterId,
                c.StudentId,
                c.AttendanceDate,
                PreviousStatus = c.PreviousStatus.ToString(),
                NewStatus = c.NewStatus.ToString(),
                c.Reason,
                c.CorrectedBy,
                c.CorrectedAtUtc
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }
}
