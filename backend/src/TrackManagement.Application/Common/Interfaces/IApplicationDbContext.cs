using Microsoft.EntityFrameworkCore;

using TrackManagement.Domain.Artists;
using TrackManagement.Domain.Dsps;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Artist> Artists { get; }

    DbSet<Track> Tracks { get; }

    DbSet<Dsp> Dsps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
