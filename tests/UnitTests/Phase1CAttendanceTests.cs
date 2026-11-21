using SchoolOS.Modules.Attendance.Application;
using SchoolOS.Modules.Attendance.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public sealed class Phase1CAttendanceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid SchoolId = Guid.CreateVersion7();
    private static readonly Guid CampusId = Guid.CreateVersion7();

    [Fact]
    public void StaffMember_Create_ShouldNormalizeEmployeeCodeAndEmail()
    {
        var staff = StaffMember.Create(
            TenantId,
            SchoolId,
            CampusId,
            "  emp-0042  ",
            "  Sana ",
            " Javed ",
            StaffRoleType.HomeroomTeacher,
            email: "  Sana.Javed@School.PK ",
            phone: " +923001234567 ");

        staff.EmployeeCode.ShouldBe("EMP-0042");
        staff.FirstName.ShouldBe("Sana");
        staff.LastName.ShouldBe("Javed");
        staff.Email.ShouldBe("sana.javed@school.pk");
        staff.Phone.ShouldBe("+923001234567");
        staff.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void AttendanceEntry_Correct_ShouldRequireReasonOnStatusChange_AndEmitAuditRecord()
    {
        var registerId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var date = new DateOnly(2026, 11, 21);
        var now = DateTimeOffset.UtcNow;

        var entry = AttendanceEntry.Create(
            TenantId,
            CampusId,
            registerId,
            studentId,
            AttendanceStatus.Absent,
            "Not at morning assembly",
            "teacher-1",
            now);

        // Same status + same remarks -> no audit record
        var noop = entry.Correct(date, AttendanceStatus.Absent, "Not at morning assembly", null, "teacher-1", now.AddMinutes(5));
        noop.ShouldBeNull();

        // Changing status without reason -> throws ArgumentException
        Should.Throw<ArgumentException>(() =>
            entry.Correct(date, AttendanceStatus.Late, "Arrived at 08:15", "   ", "teacher-1", now.AddMinutes(10)));

        // Changing status with reason -> updates status and emits immutable audit record
        var audit = entry.Correct(date, AttendanceStatus.Late, "Arrived at 08:15", "Late school bus", "teacher-1", now.AddMinutes(10));
        audit.ShouldNotBeNull();
        audit.PreviousStatus.ShouldBe(AttendanceStatus.Absent);
        audit.NewStatus.ShouldBe(AttendanceStatus.Late);
        audit.Reason.ShouldBe("Late school bus");
        audit.CorrectedBy.ShouldBe("teacher-1");
        entry.Status.ShouldBe(AttendanceStatus.Late);
        entry.LastCorrectionReason.ShouldBe("Late school bus");
    }

    [Fact]
    public void DailyAttendanceRegister_Lock_ShouldPreventSubsequentModifications()
    {
        var register = DailyAttendanceRegister.Create(
            TenantId,
            SchoolId,
            CampusId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 11, 21),
            "teacher-1",
            DateTimeOffset.UtcNow);

        register.Lock();
        register.IsLocked.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() =>
            register.UpdateNotes("Late edit", "teacher-1", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ComputeSummary_ShouldCalculateCountsAndWeightedAttendancePercentage()
    {
        var empty = AttendanceService.ComputeSummary([]);
        empty.TotalMarks.ShouldBe(0);
        empty.AttendancePercentage.ShouldBe(0m);

        // 2 Present (2.0) + 1 Late (1.0) + 1 HalfDay (0.5) + 1 Absent (0) = 3.5 / 5 = 70.00%
        var summary = AttendanceService.ComputeSummary([
            AttendanceStatus.Present,
            AttendanceStatus.Present,
            AttendanceStatus.Late,
            AttendanceStatus.HalfDay,
            AttendanceStatus.Absent
        ]);

        summary.TotalMarks.ShouldBe(5);
        summary.PresentCount.ShouldBe(2);
        summary.LateCount.ShouldBe(1);
        summary.HalfDayCount.ShouldBe(1);
        summary.AbsentCount.ShouldBe(1);
        summary.ExcusedCount.ShouldBe(0);
        summary.AttendancePercentage.ShouldBe(70.00m);
    }
}
