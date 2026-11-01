using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrackManagement.Application.Dsps;

namespace TrackManagement.Api.Controllers;

[Route("api/dsps")]
public sealed class DspsController(ISender sender) : ApiController
{
    /// <summary>List the DSPs tracks can be distributed to.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<DspResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListDspsQuery(), cancellationToken);

        return result.Match<IActionResult>(Ok, Problem);
    }
}
