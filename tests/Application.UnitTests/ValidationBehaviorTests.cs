using FluentValidation;
using IAMS.Application.Common.Behaviors;
using MediatR;
using Xunit;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.UnitTests;

public sealed record DummyRequest(string Name);

public sealed class DummyRequestValidator : AbstractValidator<DummyRequest>
{
    public DummyRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(10);
    }
}

public class ValidationBehaviorTests
{
    private static Task<string> MarkedHandler(CancellationToken cancellationToken) => Task.FromResult("done");

    [Fact]
    public async Task ValidRequest_InvokesNext()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(
            new[] { new DummyRequestValidator() });

        var response = await behavior.Handle(
            new DummyRequest("ok"),
            MarkedHandler,
            CancellationToken.None);

        Assert.Equal("done", response);
    }

    [Fact]
    public async Task InvalidRequest_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(
            new[] { new DummyRequestValidator() });

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(
                new DummyRequest(""),
                MarkedHandler,
                CancellationToken.None));

        Assert.Contains("Name", ex.Errors.Keys);
    }

    [Fact]
    public async Task NoValidators_PassesThrough()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(
            Array.Empty<IValidator<DummyRequest>>());

        var response = await behavior.Handle(
            new DummyRequest("anything"),
            MarkedHandler,
            CancellationToken.None);

        Assert.Equal("done", response);
    }
}