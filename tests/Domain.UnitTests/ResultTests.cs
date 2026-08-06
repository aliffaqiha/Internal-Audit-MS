using IAMS.Domain.Abstractions;
using Xunit;

namespace IAMS.Domain.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsIsSuccess()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ReturnsError()
    {
        var error = new Error("Demand.NotFound", "Demand not found");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessOfT_ReturnsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureOfT_ThrowsWhenAccessingValue()
    {
        var result = Result.Failure<int>(new Error("Test", "boom"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_ConvertsValueToResult()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }
}