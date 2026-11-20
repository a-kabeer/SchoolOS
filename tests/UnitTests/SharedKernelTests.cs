using SchoolOS.BuildingBlocks.SharedKernel;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public class SharedKernelTests
{
    [Fact]
    public void Result_Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Result_Failure_ShouldHaveErrorAndIsFailureTrue()
    {
        var result = Result.Failure("Sample error occurred");
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe("Sample error occurred");
    }
}
