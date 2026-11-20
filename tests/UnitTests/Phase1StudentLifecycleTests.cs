using System.Security.Cryptography;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.Modules.Organization.Domain;
using SchoolOS.Modules.Students.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public sealed class Phase1StudentLifecycleTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid SchoolId = Guid.CreateVersion7();
    private static readonly Guid CampusId = Guid.CreateVersion7();

    [Fact]
    public void Student_Create_ShouldNormalizeAdmissionNumber_AndRecordInitialStatusHistory()
    {
        var student = Student.Create(
            TenantId,
            SchoolId,
            CampusId,
            "  adm-2026-001  ",
            "  Amina ",
            " Bibi ",
            " Khan ",
            new DateOnly(2015, 5, 10),
            "Female",
            new DateOnly(2026, 4, 1));

        student.AdmissionNumber.ShouldBe("ADM-2026-001");
        student.FirstName.ShouldBe("Amina");
        student.MiddleName.ShouldBe("Bibi");
        student.LastName.ShouldBe("Khan");
        student.Status.ShouldBe(StudentLifecycleStatus.Active);
        student.StatusHistory.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(2025, 1, 1)] // 1 year old (< MinAgeYears = 3)
    [InlineData(1990, 1, 1)] // 36 years old (> MaxAgeYears = 30)
    public void Student_Create_ShouldRejectAgeOutOfBounds(int birthYear, int birthMonth, int birthDay)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Student.Create(
                TenantId,
                SchoolId,
                CampusId,
                "ADM-AGE",
                "Test",
                null,
                "Student",
                new DateOnly(birthYear, birthMonth, birthDay),
                null,
                new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void Student_StatusTransitions_ShouldAllowValidFlow_AndRejectTransitionsOutOfGraduated()
    {
        var student = Student.Create(
            TenantId,
            SchoolId,
            CampusId,
            "ADM-2026-002",
            "Bilal",
            null,
            "Ahmed",
            new DateOnly(2014, 3, 15),
            "Male",
            new DateOnly(2026, 4, 1));

        var now = DateTimeOffset.UtcNow;
        student.ChangeStatus(StudentLifecycleStatus.Inactive, new DateOnly(2026, 5, 1), "Temporary medical leave", "admin", now);
        student.Status.ShouldBe(StudentLifecycleStatus.Inactive);

        student.ChangeStatus(StudentLifecycleStatus.Active, new DateOnly(2026, 6, 1), "Returned to school", "admin", now.AddDays(1));
        student.Status.ShouldBe(StudentLifecycleStatus.Active);

        student.ChangeStatus(StudentLifecycleStatus.Graduated, new DateOnly(2027, 3, 31), "Completed Grade 10", "admin", now.AddDays(2));
        student.Status.ShouldBe(StudentLifecycleStatus.Graduated);
        student.StatusHistory.Count.ShouldBe(3);

        // Graduated is terminal
        Should.Throw<InvalidOperationException>(() =>
            student.ChangeStatus(StudentLifecycleStatus.Active, new DateOnly(2027, 4, 1), "Cannot reactivate graduate", "admin", now.AddDays(3)));
    }

    [Fact]
    public void Guardian_And_StudentGuardian_ShouldPreserveExplicitAuthorityFlags()
    {
        var guardian = Guardian.Create(
            TenantId,
            "Salman",
            "Tariq",
            "+923001112233",
            null,
            "salman@example.test",
            "Engineer",
            "SMS");

        var studentId = Guid.CreateVersion7();
        var link = StudentGuardian.Create(
            TenantId,
            studentId,
            guardian.Id,
            GuardianRelationshipType.Father,
            isPrimaryContact: true,
            isEmergencyContact: true,
            hasLegalCustody: true,
            canCollectStudent: true,
            receivesAcademicNotices: true,
            receivesFinancialNotices: true);

        link.RelationshipType.ShouldBe(GuardianRelationshipType.Father);
        link.IsPrimaryContact.ShouldBeTrue();
        link.HasLegalCustody.ShouldBeTrue();
        link.CanCollectStudent.ShouldBeTrue();
        link.ReceivesFinancialNotices.ShouldBeTrue();

        link.SetPrimary(false);
        link.Deactivate();

        link.IsPrimaryContact.ShouldBeFalse();
        link.IsActive.ShouldBeFalse();
        link.Activate();
        link.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Household_ShouldSupportMultipleSiblings_AndGuardianMembers()
    {
        var household = Household.Create(TenantId, "hh-2026-01", "Tariq Family Household");
        household.HouseholdCode.ShouldBe("HH-2026-01");

        var guardianId = Guid.CreateVersion7();
        var sibling1Id = Guid.CreateVersion7();
        var sibling2Id = Guid.CreateVersion7();

        var gMember = HouseholdMember.ForGuardian(TenantId, household.Id, guardianId, "Primary Payer", isBillingMember: true);
        var s1Member = HouseholdMember.ForStudent(TenantId, household.Id, sibling1Id, "Sibling 1");
        var s2Member = HouseholdMember.ForStudent(TenantId, household.Id, sibling2Id, "Sibling 2");

        gMember.GuardianId.ShouldBe(guardianId);
        gMember.StudentId.ShouldBeNull();
        gMember.IsBillingMember.ShouldBeTrue();

        s1Member.StudentId.ShouldBe(sibling1Id);
        s1Member.GuardianId.ShouldBeNull();
        s2Member.StudentId.ShouldBe(sibling2Id);
        s2Member.GuardianId.ShouldBeNull();
    }

    [Fact]
    public void Enrollment_Transfer_ShouldClosePreviousPlacement_AndLinkNewActivePlacement()
    {
        var studentId = Guid.CreateVersion7();
        var yearId = Guid.CreateVersion7();
        var gradeId = Guid.CreateVersion7();
        var sectionA = Guid.CreateVersion7();
        var sectionB = Guid.CreateVersion7();

        var first = Enrollment.Create(
            TenantId,
            SchoolId,
            CampusId,
            studentId,
            yearId,
            gradeId,
            sectionA,
            "101",
            new DateOnly(2026, 4, 1));

        first.Status.ShouldBe(EnrollmentStatus.Active);
        first.EndDate.ShouldBeNull();

        first.MarkTransferred(new DateOnly(2026, 8, 15), "Section balance transfer");
        first.Status.ShouldBe(EnrollmentStatus.Transferred);
        first.EndDate.ShouldBe(new DateOnly(2026, 8, 15));

        var second = Enrollment.Create(
            TenantId,
            SchoolId,
            CampusId,
            studentId,
            yearId,
            gradeId,
            sectionB,
            "204",
            new DateOnly(2026, 8, 16),
            previousEnrollmentId: first.Id,
            reason: "Section balance transfer");

        second.Status.ShouldBe(EnrollmentStatus.Active);
        second.PreviousEnrollmentId.ShouldBe(first.Id);
        second.SectionId.ShouldBe(sectionB);
    }

    [Fact]
    public void AcademicYear_ShouldRejectEndDateBeforeStartDate()
    {
        Should.Throw<ArgumentException>(() =>
            AcademicYear.Create(
                TenantId,
                SchoolId,
                "AY26",
                "Invalid Year",
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 8, 1),
                isCurrent: true));
    }

    [Fact]
    public void OfflineLicenseLeaseVerifier_ShouldEnforceNodeBinding_SequenceMonotonicity_Grace_AndClockRollback()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = ecdsa.ExportPkcs8PrivateKeyPem();
        var publicPem = ecdsa.ExportSubjectPublicKeyInfoPem();

        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var nodeId = Guid.CreateVersion7();
        var otherNodeId = Guid.CreateVersion7();

        var payload = new LicenseLeasePayload(
            TenantId: TenantId,
            IssuedAtUtc: now.AddDays(-1),
            ExpiresAtUtc: now.AddDays(30),
            KeyId: "key-2026",
            PlanCode: "PRO",
            EnabledModules: [ModuleCode.Students, ModuleCode.Admissions],
            SequenceNumber: 5,
            NodeId: nodeId,
            PaidThroughUtc: now.AddDays(10),
            GraceUntilUtc: now.AddDays(20));

        var signed = OfflineLicenseLeaseVerifier.SignWithPrivateKey(payload, privatePem);

        // 1. Valid active lease
        var validResult = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now, expectedNodeId: nodeId, minimumSequenceNumber: 5, lastTrustedUtc: now.AddMinutes(-1));
        validResult.IsValid.ShouldBeTrue();
        validResult.Status.ShouldBe(LicenseLeaseStatus.Valid);
        validResult.EntitledModules.ShouldContain(ModuleCode.Students);

        // 2. Grace period (after PaidThroughUtc, before GraceUntilUtc)
        var graceResult = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now.AddDays(15), expectedNodeId: nodeId);
        graceResult.IsValid.ShouldBeTrue();
        graceResult.Status.ShouldBe(LicenseLeaseStatus.Grace);

        // 3. Restricted period (after GraceUntilUtc, before ExpiresAtUtc)
        var restrictedResult = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now.AddDays(25), expectedNodeId: nodeId);
        restrictedResult.IsValid.ShouldBeFalse();
        restrictedResult.Status.ShouldBe(LicenseLeaseStatus.Restricted);

        // 4. NodeId mismatch rejected
        var wrongNode = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now, expectedNodeId: otherNodeId);
        wrongNode.IsValid.ShouldBeFalse();
        wrongNode.FailureReason.ShouldBe("NODE_MISMATCH");

        // 5. Monotonic sequence rollback rejected
        var rollbackSeq = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now, expectedNodeId: nodeId, minimumSequenceNumber: 6);
        rollbackSeq.IsValid.ShouldBeFalse();
        rollbackSeq.FailureReason.ShouldBe("SEQUENCE_ROLLBACK");

        // 6. Clock rollback detected
        var clockRollback = OfflineLicenseLeaseVerifier.Verify(signed, TenantId, publicPem, now.AddHours(-2), expectedNodeId: nodeId, lastTrustedUtc: now);
        clockRollback.IsValid.ShouldBeFalse();
        clockRollback.Status.ShouldBe(LicenseLeaseStatus.ClockRollbackReview);
        clockRollback.FailureReason.ShouldBe("CLOCK_ROLLBACK_DETECTED");
    }
}
