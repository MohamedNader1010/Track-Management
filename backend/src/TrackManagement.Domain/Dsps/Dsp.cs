using ErrorOr;

using TrackManagement.Domain.Common;

namespace TrackManagement.Domain.Dsps;

/// <summary>Digital Service Provider — Spotify, Apple Music, YouTube Music, Anghami...</summary>
public sealed class Dsp : Entity<Guid>
{
    public const int NameMaxLength = 100;

    private Dsp(Guid id, string name) : base(id) => Name = name;

    private Dsp() { }

    public string Name { get; private set; } = null!;

    public static Dsp Create(string name) => new(Guid.CreateVersion7(), name.Trim());
}

public static class DspErrors
{
    public static Error NotFound(IEnumerable<Guid> dspIds) =>
        Error.NotFound("Dsp.NotFound", $"Unknown DSP id(s): {string.Join(", ", dspIds)}.");
}
