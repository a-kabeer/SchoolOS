using SchoolOS.Modules.Organization.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class SchoolAndCampusDomainTests
{
    [Fact]
    public void School_Create_ValidParameters_ShouldInstantiateWithUUIDv7()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "Army Public School", "APS-01");

        school.Id.ShouldNotBe(Guid.Empty);
        school.TenantId.ShouldBe(tenantId);
        school.Name.ShouldBe("Army Public School");
        school.Code.ShouldBe("APS-01");
        school.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void School_Create_EmptyTenantId_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => School.Create(Guid.Empty, "Valid School", "VS-01"));
        ex.Message.ShouldContain("Tenant ID cannot be empty");
    }

    [Fact]
    public void School_AddCampus_ShouldAlignTenantIdWithSchool()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "City School", "CS-01");

        var campus = school.AddCampus("Main Campus", "MAIN", "Sector F-8, Islamabad");

        campus.ShouldNotBeNull();
        campus.TenantId.ShouldBe(tenantId);
        campus.SchoolId.ShouldBe(school.Id);
        campus.Name.ShouldBe("Main Campus");
        campus.Code.ShouldBe("MAIN");
        school.Campuses.ShouldContain(campus);
    }

    [Fact]
    public void Campus_CreateForSchool_ShouldEnforceSameTenantId()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "Beaconhouse", "BSS-01");

        var campus = Campus.CreateForSchool(school, "Gulberg Branch", "GUL");

        campus.TenantId.ShouldBe(tenantId);
        campus.SchoolId.ShouldBe(school.Id);
    }
}
