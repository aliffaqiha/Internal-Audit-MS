using FluentValidation.TestHelper;
using IAMS.Application.CorrectiveActions;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Xunit;

namespace IAMS.Application.UnitTests;

public class CapStateTests
{
    [Theory]
    [InlineData(CorrectiveActionStatus.Open, CorrectiveActionStatus.InProgress)]
    [InlineData(CorrectiveActionStatus.InProgress, CorrectiveActionStatus.PendingVerification)]
    [InlineData(CorrectiveActionStatus.PendingVerification, CorrectiveActionStatus.Closed)]
    [InlineData(CorrectiveActionStatus.PendingVerification, CorrectiveActionStatus.InProgress)] // rejected => reopen
    public void ValidTransition_DoesNotThrow(CorrectiveActionStatus from, CorrectiveActionStatus to)
    {
        var cap = new CorrectiveAction { Status = from };

        CapState.EnsureTransition(cap, from, to, "transition");
    }

    [Fact]
    public void OpenToPendingVerification_Throws()
    {
        var cap = new CorrectiveAction { Status = CorrectiveActionStatus.Open };

        Assert.Throws<InvalidOperationException>(() =>
            CapState.EnsureTransition(cap, CorrectiveActionStatus.InProgress, CorrectiveActionStatus.PendingVerification, "ajukan"));
    }

    [Fact]
    public void ClosedToAnything_Throws()
    {
        var cap = new CorrectiveAction { Status = CorrectiveActionStatus.Closed };

        Assert.Throws<InvalidOperationException>(() =>
            CapState.EnsureTransition(cap, CorrectiveActionStatus.PendingVerification, CorrectiveActionStatus.Closed, "verify"));
    }
}

public class CreateCapCommandValidatorTests
{
    private readonly CreateCapCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.TestValidate(new CreateCapCommand(
            Guid.NewGuid(), "Perbaiki backup", "Budi", new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 0));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyAction_Fails()
    {
        var result = _validator.TestValidate(new CreateCapCommand(Guid.NewGuid(), "", null, null, 0));
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void OutOfRangeProgress_Fails(int progress)
    {
        var result = _validator.TestValidate(new CreateCapCommand(Guid.NewGuid(), "x", null, null, progress));
        result.ShouldHaveValidationErrorFor(x => x.Progress);
    }
}

public class UpdateCapCommandValidatorTests
{
    private readonly UpdateCapCommandValidator _validator = new();

    [Theory]
    [InlineData(-5)]
    [InlineData(120)]
    public void OutOfRangeProgress_Fails(int progress)
    {
        var result = _validator.TestValidate(new UpdateCapCommand("x", null, null, progress, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Progress);
    }
}

public class VerifyCapCommandValidatorTests
{
    private readonly VerifyCapCommandValidator _validator = new();

    [Fact]
    public void TooLongNote_Fails()
    {
        var result = _validator.TestValidate(new VerifyCapCommand(true, new string('x', 1001), Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}