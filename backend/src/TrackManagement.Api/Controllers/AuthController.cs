using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using TrackManagement.Api.Contracts;
using TrackManagement.Api.Common;
using TrackManagement.Application.Auth;

namespace TrackManagement.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    /// <summary>Exchange demo credentials for a JWT access token.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Auth)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Token(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Username, request.Password), cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Who am I? Any valid token works — handy to check a token from the UI or Scalar.</summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var roles = User.FindAll(TrackManagement.Infrastructure.Authentication.AuthClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new CurrentUserResponse(User.Identity?.Name ?? string.Empty, roles));
    }
}
