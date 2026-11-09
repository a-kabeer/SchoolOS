using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using SchoolOS.BuildingBlocks.Security;
using SchoolOS.BuildingBlocks.SharedKernel;
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
                "SchoolOS.PlatformModules.Tenants")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
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
                "SchoolOS.PlatformModules.Tenants")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void TwoPlane_Separation_LocalApi_ShouldNotDependOn_PlatformModules()
    {
        var localAssembly = Assembly.Load("SchoolOS.Local.Api");
        var result = Types.InAssembly(localAssembly)
            .ShouldNot()
            .HaveDependencyOn("SchoolOS.PlatformModules.Tenants")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void TwoPlane_Separation_CloudApi_ShouldNotDependOn_LocalModules()
    {
        var cloudAssembly = Assembly.Load("SchoolOS.Cloud.Api");
        var result = Types.InAssembly(cloudAssembly)
            .ShouldNot()
            .HaveDependencyOn("SchoolOS.Modules.Organization")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
