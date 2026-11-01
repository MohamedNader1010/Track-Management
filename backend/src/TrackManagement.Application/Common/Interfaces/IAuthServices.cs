namespace TrackManagement.Application.Common.Interfaces;

public sealed record AuthenticatedUser(string Username, IReadOnlyCollection<string> Roles);

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);

public interface IUserAuthenticator
{
    /// <returns>The user, or <c>null</c> when the credentials are wrong (callers must not reveal which part was wrong).</returns>
    AuthenticatedUser? Authenticate(string username, string password);
}

public interface IJwtTokenGenerator
{
    AccessToken Generate(AuthenticatedUser user);
}
