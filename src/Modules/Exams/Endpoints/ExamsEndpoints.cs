using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.Web;
using SchoolOS.Modules.Exams.Application;
using SchoolOS.Modules.Exams.Domain;
using SchoolOS.Modules.Exams.Persistence;

namespace SchoolOS.Modules.Exams.Endpoints;

public sealed class ExamsEndpointGroup : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/exams";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapNamedGet(ListSubjects, "subjects")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateSubject, "subjects")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListExamTerms, "terms")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateExamTerm, "terms")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListExamPapers, "papers")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
        groupBuilder.MapNamedPost(CreateExamPaper, "papers")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Create);

        groupBuilder.MapNamedGet(ListMarkSheets, "marksheets")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
        groupBuilder.MapNamedGet(GetSectionMarkSheet, "papers/{paperId:guid}/sections/{sectionId:guid}/marksheet")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
        groupBuilder.MapNamedPost(SaveSectionMarks, "papers/{paperId:guid}/sections/{sectionId:guid}/marks")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Create);

        groupBuilder.MapNamedPost(SubmitMarkSheet, "marksheets/{markSheetId:guid}/submit")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Edit);
        groupBuilder.MapNamedPost(ReviewMarkSheet, "marksheets/{markSheetId:guid}/review")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.Edit);

        groupBuilder.MapNamedGet(GetStudentReportCard, "terms/{termId:guid}/students/{studentId:guid}/report-card")
            .RequireModulePermission(ModuleCode.Exams, PermissionAction.View);
    }

    private static async Task<IResult> ListSubjects(ExamsDbContext db, Guid? gradeLevelId, CancellationToken ct)
    {
        var query = db.Subjects.AsNoTracking();
        if (gradeLevelId.HasValue) query = query.Where(s => s.GradeLevelId == gradeLevelId.Value);

        var items = await query
            .OrderBy(s => s.Code)
            .Select(s => new { s.Id, s.SchoolId, s.GradeLevelId, s.Code, s.Name, s.IsActive })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateSubject(CreateSubjectCommand request, ExamsService service, CancellationToken ct)
    {
        try
        {
            var subject = await service.CreateSubjectAsync(request, ct);
            return Results.Created(
                $"/api/v1/exams/subjects/{subject.Id}",
                new { subject.Id, subject.SchoolId, subject.GradeLevelId, subject.Code, subject.Name, subject.IsActive });
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "GRADE_NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "INVALID_SUBJECT", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "SUBJECT_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> ListExamTerms(
        ExamsDbContext db,
        ITenantContext tenantContext,
        Guid? campusId,
        Guid? academicYearId,
        CancellationToken ct)
    {
        var query = db.ExamTerms.AsNoTracking();
        if (tenantContext.CampusId.HasValue) query = query.Where(t => t.CampusId == tenantContext.CampusId.Value);
        else if (campusId.HasValue) query = query.Where(t => t.CampusId == campusId.Value);
        if (academicYearId.HasValue) query = query.Where(t => t.AcademicYearId == academicYearId.Value);

        var items = await query
            .OrderByDescending(t => t.StartDate)
            .Select(t => new { t.Id, t.SchoolId, t.CampusId, t.AcademicYearId, t.Code, t.Name, t.StartDate, t.EndDate, Status = t.Status.ToString() })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateExamTerm(CreateExamTermCommand request, ExamsService service, CancellationToken ct)
    {
        try
        {
            var term = await service.CreateExamTermAsync(request, ct);
            return Results.Created(
                $"/api/v1/exams/terms/{term.Id}",
                new { term.Id, term.SchoolId, term.CampusId, term.AcademicYearId, term.Code, term.Name, term.StartDate, term.EndDate, Status = term.Status.ToString() });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "INVALID_EXAM_TERM", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "EXAM_TERM_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> ListExamPapers(
        ExamsDbContext db,
        ITenantContext tenantContext,
        Guid? examTermId,
        Guid? gradeLevelId,
        CancellationToken ct)
    {
        var query = from p in db.ExamPapers.AsNoTracking()
                    join s in db.Subjects.AsNoTracking() on p.SubjectId equals s.Id
                    join t in db.ExamTerms.AsNoTracking() on p.ExamTermId equals t.Id
                    select new
                    {
                        p.Id,
                        p.CampusId,
                        p.ExamTermId,
                        ExamTermName = t.Name,
                        p.GradeLevelId,
                        p.SubjectId,
                        SubjectCode = s.Code,
                        SubjectName = s.Name,
                        p.MaxMarks,
                        p.PassingMarks,
                        p.WeightagePercent,
                        p.ExamDate
                    };

        if (tenantContext.CampusId.HasValue) query = query.Where(x => x.CampusId == tenantContext.CampusId.Value);
        if (examTermId.HasValue) query = query.Where(x => x.ExamTermId == examTermId.Value);
        if (gradeLevelId.HasValue) query = query.Where(x => x.GradeLevelId == gradeLevelId.Value);

        return Results.Ok(await query.OrderBy(x => x.SubjectCode).ToListAsync(ct));
    }

    private static async Task<IResult> CreateExamPaper(CreateExamPaperCommand request, ExamsService service, CancellationToken ct)
    {
        try
        {
            var paper = await service.CreateExamPaperAsync(request, ct);
            return Results.Created(
                $"/api/v1/exams/papers/{paper.Id}",
                new { paper.Id, paper.ExamTermId, paper.GradeLevelId, paper.SubjectId, paper.MaxMarks, paper.PassingMarks, paper.WeightagePercent, paper.ExamDate });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "INVALID_EXAM_PAPER", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "EXAM_PAPER_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> ListMarkSheets(
        ExamsDbContext db,
        ITenantContext tenantContext,
        MarkSheetStatus? status,
        CancellationToken ct)
    {
        var query = from m in db.MarkSheets.AsNoTracking()
                    join p in db.ExamPapers.AsNoTracking() on m.ExamPaperId equals p.Id
                    join s in db.Subjects.AsNoTracking() on p.SubjectId equals s.Id
                    join t in db.ExamTerms.AsNoTracking() on p.ExamTermId equals t.Id
                    select new
                    {
                        m.Id,
                        m.CampusId,
                        m.ExamPaperId,
                        m.SectionId,
                        ExamTermName = t.Name,
                        SubjectName = s.Name,
                        p.MaxMarks,
                        Status = m.Status,
                        m.SubmittedBy,
                        m.SubmittedAtUtc,
                        m.ApprovedBy,
                        m.ApprovedAtUtc,
                        m.ReviewNotes
                    };

        if (tenantContext.CampusId.HasValue) query = query.Where(x => x.CampusId == tenantContext.CampusId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var list = await query
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.CampusId,
                x.ExamPaperId,
                x.SectionId,
                x.ExamTermName,
                x.SubjectName,
                x.MaxMarks,
                Status = x.Status.ToString(),
                x.SubmittedBy,
                x.SubmittedAtUtc,
                x.ApprovedBy,
                x.ApprovedAtUtc,
                x.ReviewNotes
            })
            .ToListAsync(ct);

        return Results.Ok(list);
    }

    private static async Task<IResult> GetSectionMarkSheet(
        Guid paperId,
        Guid sectionId,
        HttpContext httpContext,
        ExamsService service,
        CancellationToken ct)
    {
        try
        {
            var sheet = await service.GetSectionMarkSheetAsync(paperId, sectionId, httpContext.User, ct);
            return Results.Ok(sheet);
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "INVALID_SECTION", message = ex.Message }); }
    }

    private static async Task<IResult> SaveSectionMarks(
        Guid paperId,
        Guid sectionId,
        SaveSectionMarksCommand request,
        HttpContext httpContext,
        ExamsService service,
        CancellationToken ct)
    {
        try
        {
            var sheet = await service.SaveSectionMarksAsync(paperId, sectionId, request, httpContext.User, ct);
            return Results.Ok(new { markSheetId = sheet.Id, status = sheet.Status.ToString(), marksCount = sheet.Marks.Count });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "INVALID_MARKS", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "MARKSHEET_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> SubmitMarkSheet(
        Guid markSheetId,
        HttpContext httpContext,
        ExamsService service,
        CancellationToken ct)
    {
        try
        {
            var sheet = await service.SubmitMarkSheetAsync(markSheetId, httpContext.User, ct);
            return Results.Ok(new { sheet.Id, Status = sheet.Status.ToString(), sheet.SubmittedBy, sheet.SubmittedAtUtc });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "MARKSHEET_NOT_FOUND", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "MARKSHEET_SUBMIT_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> ReviewMarkSheet(
        Guid markSheetId,
        ReviewMarkSheetCommand request,
        HttpContext httpContext,
        ExamsService service,
        CancellationToken ct)
    {
        try
        {
            var sheet = await service.ReviewMarkSheetAsync(markSheetId, request, httpContext.User, ct);
            return Results.Ok(new { sheet.Id, Status = sheet.Status.ToString(), sheet.ApprovedBy, sheet.ApprovedAtUtc, sheet.ReviewNotes });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "MARKSHEET_NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "INVALID_REVIEW", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "MARKSHEET_REVIEW_CONFLICT", message = ex.Message }); }
    }

    private static async Task<IResult> GetStudentReportCard(
        Guid termId,
        Guid studentId,
        ExamsService service,
        CancellationToken ct)
    {
        try
        {
            var card = await service.GetStudentReportCardAsync(termId, studentId, ct);
            return Results.Ok(card);
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { code = "EXAM_TERM_NOT_FOUND", message = ex.Message }); }
    }
}
