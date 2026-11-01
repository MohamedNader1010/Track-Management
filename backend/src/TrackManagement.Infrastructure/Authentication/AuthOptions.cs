using System.ComponentModel.DataAnnotations;

namespace TrackManagement.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 key. 256 bits minimum — never committed for real environments (user-secrets / env vars).</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters (256 bits).")]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}

/// <summary>
/// Demo identity store for the assessment. A real system would delegate to an identity provider
/// (Keycloak / Entra ID) and this API would only validate tokens.
/// </summary>
public sealed class DemoUsersOptions
{
    public const string SectionName = "DemoUsers";

    public List<DemoUser> Users { get; init; } = [];
}

public sealed class DemoUser
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = [];
}
