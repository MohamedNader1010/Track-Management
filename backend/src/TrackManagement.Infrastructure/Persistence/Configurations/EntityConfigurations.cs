using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TrackManagement.Domain.Artists;
using TrackManagement.Domain.Dsps;
using TrackManagement.Domain.Tracks;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");
        builder.HasKey(a => a.Id);
        // Ids are generated in the domain (Guid v7) — EF must not treat a non-empty key as "already saved".
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name).HasMaxLength(Artist.NameMaxLength).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(Artist.EmailMaxLength).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(Artist.CountryCodeLength).IsFixedLength().IsRequired();

        builder.HasIndex(a => a.Email).IsUnique();
        builder.Ignore(a => a.DomainEvents);
    }
}

internal sealed class DspConfiguration : IEntityTypeConfiguration<Dsp>
{
    public void Configure(EntityTypeBuilder<Dsp> builder)
    {
        builder.ToTable("Dsps");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).HasMaxLength(Dsp.NameMaxLength).IsRequired();
        builder.HasIndex(d => d.Name).IsUnique();
        builder.Ignore(d => d.DomainEvents);
    }
}

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("Tracks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title).HasMaxLength(Track.TitleMaxLength).IsRequired();

        builder.Property(t => t.Isrc)
            .HasConversion(isrc => isrc.Value, value => Isrc.Create(value).Value)
            .HasMaxLength(Isrc.Length)
            .IsFixedLength()
            .IsRequired();
        builder.HasIndex(t => t.Isrc).IsUnique();

        // Smart enums are persisted by name: readable in SQL and stable if values are renumbered.
        builder.Property(t => t.Genre)
            .HasConversion(genre => genre.Name, name => Genre.FromName(name, false))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion(status => status.Name, name => TrackStatus.FromName(name, false))
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Genre);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(t => t.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Distributions)
            .WithOne()
            .HasForeignKey(d => d.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Distributions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic concurrency without leaking persistence concerns into the domain model.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(t => t.DomainEvents);
    }
}

internal sealed class TrackDistributionConfiguration : IEntityTypeConfiguration<TrackDistribution>
{
    public void Configure(EntityTypeBuilder<TrackDistribution> builder)
    {
        builder.ToTable("TrackDistributions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Status)
            .HasConversion(status => status.Name, name => DistributionStatus.FromName(name, false))
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<Dsp>()
            .WithMany()
            .HasForeignKey(d => d.DspId)
            .OnDelete(DeleteBehavior.Restrict);

        // One delivery row per (track, DSP); a rejected delivery is re-used when re-submitted.
        builder.HasIndex(d => new { d.TrackId, d.DspId }).IsUnique();

        builder.Ignore(d => d.DomainEvents);
    }
}
