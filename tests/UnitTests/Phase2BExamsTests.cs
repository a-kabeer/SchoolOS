using SchoolOS.Modules.Exams.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public sealed class Phase2BExamsTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid SchoolId = Guid.CreateVersion7();
    private static readonly Guid CampusId = Guid.CreateVersion7();

    [Theory]
    [InlineData(95, 100, true, "A+")]
    [InlineData(82, 100, true, "A")]
    [InlineData(74, 100, true, "B")]
    [InlineData(61, 100, true, "C")]
    [InlineData(50, 100, true, "D")]
    [InlineData(45, 100, false, "F")]
    [InlineData(85, 100, false, "F")] // Failed subject (e.g. absent) always receives F
    public void ExamGrading_ShouldComputeExpectedLetterGrades(decimal obtained, decimal max, bool passed, string expected)
    {
        var pct = ExamGrading.CalculatePercentage(obtained, max);
        ExamGrading.CalculateLetterGrade(pct, passed).ShouldBe(expected);
    }

    [Fact]
    public void ExamPaper_ShouldRejectInvalidPassingOrMaxMarks()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ExamPaper.Create(TenantId, SchoolId, CampusId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 0m, 0m));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ExamPaper.Create(TenantId, SchoolId, CampusId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 100m, 105m));
    }

    [Fact]
    public void StudentMark_ShouldEnforceBounds_AndZeroScoreWhenAbsent()
    {
        var sheetId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            StudentMark.Create(TenantId, CampusId, sheetId, studentId, 101m, 100m, isAbsent: false));

        var absent = StudentMark.Create(TenantId, CampusId, sheetId, studentId, 50m, 100m, isAbsent: true, remarks: "Medical leave");
        absent.IsAbsent.ShouldBeTrue();
        absent.MarksObtained.ShouldBe(0m);
    }

    [Fact]
    public void MarkSheet_Workflow_ShouldEnforceDraftSubmittedReturnedAndImmutableApprovedStates()
    {
        var sheet = MarkSheet.Create(TenantId, SchoolId, CampusId, Guid.CreateVersion7(), Guid.CreateVersion7());
        var now = DateTimeOffset.UtcNow;

        sheet.Status.ShouldBe(MarkSheetStatus.Draft);
        sheet.IsEditable.ShouldBeTrue();

        // Cannot approve directly from Draft
        Should.Throw<InvalidOperationException>(() => sheet.Approve("principal", null, now));

        sheet.Submit("teacher-1", now);
        sheet.Status.ShouldBe(MarkSheetStatus.Submitted);
        sheet.IsEditable.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => sheet.EnsureEditable());

        // Return for correction requires non-empty notes
        Should.Throw<ArgumentException>(() => sheet.ReturnForCorrection("principal", "   ", now));

        sheet.ReturnForCorrection("principal", "Verify Q3 tally", now.AddMinutes(5));
        sheet.Status.ShouldBe(MarkSheetStatus.ReturnedForCorrection);
        sheet.IsEditable.ShouldBeTrue();

        // Resubmit and approve
        sheet.Submit("teacher-1", now.AddMinutes(10));
        sheet.Approve("principal", "Verified", now.AddMinutes(15));
        sheet.Status.ShouldBe(MarkSheetStatus.Approved);
        sheet.IsEditable.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => sheet.EnsureEditable());
    }
}
