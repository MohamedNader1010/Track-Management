using TrackManagement.Domain.Common;

namespace TrackManagement.Domain.Artists;

public sealed class Artist : AuditableEntity<Guid>
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int CountryCodeLength = 2;

    private Artist(Guid id, string name, string email, string country) : base(id)
    {
        Name = name;
        Email = email;
        Country = country;
    }

    private Artist() { }

    public string Name { get; private set; } = null!;

    /// <summary>Stored normalised (trimmed, lower-case) so uniqueness checks are case-insensitive.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>ISO 3166-1 alpha-2 code, upper-case (EG, SA, AE...).</summary>
    public string Country { get; private set; } = null!;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static Artist Create(string name, string email, string country)
    {
        var artist = new Artist(
            Guid.CreateVersion7(),
            name.Trim(),
            NormalizeEmail(email),
            country.Trim().ToUpperInvariant());

        artist.Raise(new ArtistCreatedDomainEvent(artist.Id, artist.Name));
        return artist;
    }
}

public sealed record ArtistCreatedDomainEvent(Guid ArtistId, string Name) : IDomainEvent;
