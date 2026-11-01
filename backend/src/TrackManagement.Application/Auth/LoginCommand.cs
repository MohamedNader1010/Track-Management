using ErrorOr;

using FluentValidation;

using MediatR;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Application.Auth;

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);

public sealed record LoginCommand(string Username, string Password) : IRequest<ErrorOr<TokenResponse>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginCommandHandler(IUserAuthenticator authenticator, IJwtTokenGenerator tokenGenerator)
    : IRequestHandler<LoginCommand, ErrorOr<TokenResponse>>
{
    // One generic message for unknown user AND wrong password, so usernames can't be enumerated.
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password.");

    public Task<ErrorOr<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = authenticator.Authenticate(request.Username, request.Password);
        if (user is null)
        {
            return Task.FromResult<ErrorOr<TokenResponse>>(InvalidCredentials);
        }

        var token = tokenGenerator.Generate(user);
        return Task.FromResult<ErrorOr<TokenResponse>>(new TokenResponse(token.Token, "Bearer", token.ExpiresAtUtc));
    }
}
