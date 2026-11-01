using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using TrackManagement.Application.Common.Interfaces;

namespace TrackManagement.Infrastructure.Authentication;

internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken Generate(AuthenticatedUser user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpiryMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            .. user.Roles.Select(role => new Claim(AuthClaimTypes.Role, role)),
        ];

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }
}

public static class AuthClaimTypes
{
    public const string Role = "role";
    public const string Name = JwtRegisteredClaimNames.Sub;
}
