using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Infrastructure.Authentication;

internal sealed class DemoUserAuthenticator(IOptionsMonitor<DemoUsersOptions> options) : IUserAuthenticator
{
    public AuthenticatedUser? Authenticate(string username, string password)
    {
        var user = options.CurrentValue.Users
            .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        // Always do the comparison (against a dummy when the user doesn't exist) so response time
        // doesn't reveal whether the username is valid.
        var expected = user?.Password ?? "\0no-such-user\0";
        var passwordMatches = FixedTimeEquals(expected, password);

        return user is not null && passwordMatches
            ? new AuthenticatedUser(user.Username, user.Roles)
            : null;
    }

    // Hash both sides first so the comparison is constant-time regardless of input lengths.
    private static bool FixedTimeEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
}
