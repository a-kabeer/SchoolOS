using System.Reflection;
using NetArchTest.Rules;
using SchoolOS.BuildingBlocks.Localization;
using SchoolOS.BuildingBlocks.Observability;
using SchoolOS.BuildingBlocks.Secrets;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;
using SchoolOS.BuildingBlocks.Web;
using Shouldly;
using Xunit;

namespace SchoolOS.ArchitectureTests;

public class ModuleBoundaryTests
{
    [Fact]
    public void SharedKernel_ShouldNotDependOn_HostsOrModules()
    {
        var result = Types.InAssembly(typeof(Entity<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.Cloud.Api",
                "SchoolOS.Local.Api",
                "SchoolOS.Modules.Organization",
                "SchoolOS.Modules.Identity",
                "SchoolOS.Modules.Admissions",
                "SchoolOS.PlatformModules.Tenants",
                "SchoolOS.PlatformModules.Subscriptions",
                "SchoolOS.PlatformModules.Licensing")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Security_ShouldNotDependOn_HostsOrModules()
    {
        var result = Types.InAssembly(typeof(ITenantContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.Cloud.Api",
                "SchoolOS.Local.Api",
                "SchoolOS.Modules.Organization",
                "SchoolOS.Modules.Identity",
                "SchoolOS.Modules.Admissions",
                "SchoolOS.PlatformModules.Tenants",
                "SchoolOS.PlatformModules.Subscriptions",
                "SchoolOS.PlatformModules.Licensing")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void TwoPlane_Separation_LocalApi_ShouldNotDependOn_PlatformModules()
    {
        var localAssembly = Assembly.Load("SchoolOS.Local.Api");
        var result = Types.InAssembly(localAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.PlatformModules.Tenants",
                "SchoolOS.PlatformModules.Subscriptions",
                "SchoolOS.PlatformModules.Licensing")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void TwoPlane_Separation_CloudApi_ShouldNotDependOn_LocalModules()
    {
        var cloudAssembly = Assembly.Load("SchoolOS.Cloud.Api");
        var result = Types.InAssembly(cloudAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.Modules.Organization",
                "SchoolOS.Modules.Identity",
                "SchoolOS.Modules.Students",
                "SchoolOS.Modules.Admissions")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Secrets_Observability_And_Web_BuildingBlocks_ShouldNotDependOn_HostsOrModules()
    {
        string[] forbidden =
        [
            "SchoolOS.Cloud.Api",
            "SchoolOS.Local.Api",
            "SchoolOS.Modules.Organization",
            "SchoolOS.Modules.Identity",
            "SchoolOS.Modules.Students",
            "SchoolOS.Modules.Admissions",
            "SchoolOS.PlatformModules.Tenants",
            "SchoolOS.PlatformModules.Subscriptions",
            "SchoolOS.PlatformModules.Licensing"
        ];

        foreach (var assembly in new[]
        {
            typeof(ISecretProvider).Assembly,
            typeof(ObservabilityOptions).Assembly,
            typeof(IEndpointGroup).Assembly,
            typeof(SupportedLanguage).Assembly
        })
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.ShouldBeTrue($"BuildingBlock {assembly.GetName().Name} must stay decoupled from hosts and modules.");
        }
    }

    [Fact]
    public void Students_And_Admissions_ShouldNotAccess_OtherModules_DbContexts_Directly()
    {
        var studentsAssembly = Assembly.Load("SchoolOS.Modules.Students");
        var studentsResult = Types.InAssembly(studentsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.Modules.Organization.Persistence",
                "SchoolOS.Modules.Admissions.Persistence",
                "SchoolOS.Modules.Identity.Persistence")
            .GetResult();
        studentsResult.IsSuccessful.ShouldBeTrue("Students module must consume Organization via published Contracts, not OrganizationDbContext.");

        var admissionsAssembly = Assembly.Load("SchoolOS.Modules.Admissions");
        var admissionsResult = Types.InAssembly(admissionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SchoolOS.Modules.Organization.Persistence",
                "SchoolOS.Modules.Students.Persistence",
                "SchoolOS.Modules.Identity.Persistence")
            .GetResult();
        admissionsResult.IsSuccessful.ShouldBeTrue("Admissions module must consume Students/Organization via published Contracts, not DbContexts.");
    }
}
