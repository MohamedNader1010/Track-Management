using ErrorOr;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

using NSubstitute;

using TrackManagement.Application.Artists;
using TrackManagement.Application.Artists.Commands.CreateArtist;
using TrackManagement.Application.Common.Behaviors;

namespace TrackManagement.Application.UnitTests.Common;

public sealed class ValidationBehaviorTests
{
    private static readonly CreateArtistCommand Command = new("Nour", "nour@example.com", "EG");

    [Fact]
    public async Task Returns_validation_errors_and_never_calls_the_handler_when_invalid()
    {
        var validator = Substitute.For<IValidator<CreateArtistCommand>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<CreateArtistCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Email", "Email is invalid.")]));

        var next = Substitute.For<RequestHandlerDelegate<ErrorOr<ArtistResponse>>>();
        var behavior = new ValidationBehavior<CreateArtistCommand, ErrorOr<ArtistResponse>>([validator]);

        var result = await behavior.Handle(Command, next, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem();
        error.Type.ShouldBe(ErrorType.Validation);
        error.Code.ShouldBe("Email");
        await next.DidNotReceiveWithAnyArgs().Invoke();
    }

    [Fact]
    public async Task Calls_the_handler_when_valid()
    {
        var expected = new ArtistResponse(Guid.CreateVersion7(), "Nour", "nour@example.com", "EG", DateTimeOffset.UtcNow);
        var next = Substitute.For<RequestHandlerDelegate<ErrorOr<ArtistResponse>>>();
        next.Invoke().ReturnsForAnyArgs(Task.FromResult<ErrorOr<ArtistResponse>>(expected));
        var behavior = new ValidationBehavior<CreateArtistCommand, ErrorOr<ArtistResponse>>([new CreateArtistCommandValidator()]);

        var result = await behavior.Handle(Command, next, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(expected);
    }
}
