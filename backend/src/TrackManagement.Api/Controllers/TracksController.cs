using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrackManagement.Api.Contracts;
using TrackManagement.Application.Common.Models;
using TrackManagement.Application.Common.Security;
using TrackManagement.Application.Tracks;
using TrackManagement.Application.Tracks.Commands.CreateTrack;
using TrackManagement.Application.Tracks.Commands.DistributeTrack;
using TrackManagement.Application.Tracks.Commands.UpdateTrackStatus;
using TrackManagement.Application.Tracks.Queries.GetTrackById;
using TrackManagement.Application.Tracks.Queries.ListTracks;

namespace TrackManagement.Api.Controllers;

[Route("api/tracks")]
public sealed class TracksController(ISender sender) : ApiController
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<TrackSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? artistId,
        [FromQuery] string? genre,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ListTracksQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new ListTracksQuery(artistId, genre, status, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<TrackDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTrackByIdQuery(id), cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageCatalog)]
    [ProducesResponseType<TrackDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateTrackRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTrackCommand(
            request.Title,
            request.ArtistId,
            request.Isrc,
            request.ReleaseDate,
            request.Genre);

        var result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            track => CreatedAtAction(nameof(GetById), new { id = track.Id }, track),
            Problem);
    }
    
    [HttpPost("{id:guid}/distribute")]
    [Authorize(Policy = Policies.ManageCatalog)]
    [ProducesResponseType<TrackDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Distribute(Guid id, DistributeTrackRequest request, CancellationToken cancellationToken)
    {
        var command = new DistributeTrackCommand(id, request.DspIds ?? []);

        var result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }
    
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Policies.ManageCatalog)]
    [ProducesResponseType<TrackDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTrackStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTrackStatusCommand(id, request.Status), cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }
}
