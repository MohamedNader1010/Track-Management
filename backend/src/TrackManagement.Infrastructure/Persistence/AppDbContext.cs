using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Domain.Artists;
using TrackManagement.Domain.Dsps;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Artist> Artists => Set<Artist>();

    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<Dsp> Dsps => Set<Dsp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
