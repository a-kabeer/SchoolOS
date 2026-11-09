using FluentAssertions;
using SchoolOS.BuildingBlocks.SharedKernel;
using Xunit;

namespace SchoolOS.UnitTests;

public class SharedKernelTests
{
    [Fact]
    public void Result_Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_ShouldHaveErrorAndIsFailureTrue()
    {
        var result = Result.Failure("Sample error occurred");
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Sample error occurred");
    }
}
