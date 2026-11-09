using FluentAssertions;
using SchoolOS.Modules.Organization.Domain;
using Xunit;

namespace SchoolOS.UnitTests;

public class SchoolAndCampusDomainTests
{
    [Fact]
    public void School_Create_ValidParameters_ShouldInstantiateWithUUIDv7()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "Army Public School", "APS-01");

        school.Id.Should().NotBeEmpty();
        school.TenantId.Should().Be(tenantId);
        school.Name.Should().Be("Army Public School");
        school.Code.Should().Be("APS-01");
        school.IsActive.Should().BeTrue();
    }

    [Fact]
    public void School_Create_EmptyTenantId_ShouldThrowArgumentException()
    {
        var act = () => School.Create(Guid.Empty, "Valid School", "VS-01");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Tenant ID cannot be empty*");
    }

    [Fact]
    public void School_AddCampus_ShouldAlignTenantIdWithSchool()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "City School", "CS-01");

        var campus = school.AddCampus("Main Campus", "MAIN", "Sector F-8, Islamabad");

        campus.Should().NotBeNull();
        campus.TenantId.Should().Be(tenantId);
        campus.SchoolId.Should().Be(school.Id);
        campus.Name.Should().Be("Main Campus");
        campus.Code.Should().Be("MAIN");
        school.Campuses.Should().Contain(campus);
    }

    [Fact]
    public void Campus_CreateForSchool_ShouldEnforceSameTenantId()
    {
        var tenantId = Guid.CreateVersion7();
        var school = School.Create(tenantId, "Beaconhouse", "BSS-01");

        var campus = Campus.CreateForSchool(school, "Gulberg Branch", "GUL");

        campus.TenantId.Should().Be(tenantId);
        campus.SchoolId.Should().Be(school.Id);
    }
}
