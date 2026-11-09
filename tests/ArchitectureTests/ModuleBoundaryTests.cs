using FluentAssertions;
using NetArchTest.Rules;
using SchoolOS.BuildingBlocks.SharedKernel;
using Xunit;

namespace SchoolOS.ArchitectureTests;

public class ModuleBoundaryTests
{
    [Fact]
    public void SharedKernel_ShouldNotDependOn_HostsOrExternalFrameworks()
    {
        var result = Types.InAssembly(typeof(Entity<>).Assembly)
            .ShouldNot()
            .HaveDependencyOn("SchoolOS.Cloud.Api")
            .And()
            .ShouldNot()
            .HaveDependencyOn("SchoolOS.Local.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
