using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrackManagement.Api.Contracts;
using TrackManagement.Application.Artists;
using TrackManagement.Application.Artists.Commands.CreateArtist;
using TrackManagement.Application.Artists.Queries.ListArtists;
using TrackManagement.Application.Common.Security;

namespace TrackManagement.Api.Controllers;

[Route("api/artists")]
public sealed class ArtistsController(ISender sender) : ApiController
{
    /// <summary>List all artists.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<ArtistResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListArtistsQuery(), cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }

    /// <summary>Create an artist. Requires the catalog-manager role.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManageCatalog)]
    [ProducesResponseType<ArtistResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateArtistRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateArtistCommand(request.Name, request.Email, request.Country);

        var result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            artist => StatusCode(StatusCodes.Status201Created, artist),
            Problem);
    }
}
