using System.Text.RegularExpressions;

using ErrorOr;

namespace TrackManagement.Domain.Tracks;

/// <summary>
/// International Standard Recording Code — value object.
/// Format: CC-XXX-YY-NNNNN (country, registrant, year, designation), stored without hyphens (12 chars).
/// </summary>
public sealed partial record Isrc
{
    public const int Length = 12;

    private Isrc(string value) => Value = value;

    public string Value { get; }

    public static ErrorOr<Isrc> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return IsrcErrors.Empty;
        }

        var normalized = raw.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();

        return IsrcPattern().IsMatch(normalized)
            ? new Isrc(normalized)
            : IsrcErrors.InvalidFormat;
    }

    public static bool IsValid(string? raw) => !Create(raw).IsError;

    /// <summary>Human-friendly representation: EG-TKW-24-00001.</summary>
    public string ToDisplayString() => $"{Value[..2]}-{Value[2..5]}-{Value[5..7]}-{Value[7..]}";

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{3}[0-9]{7}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsrcPattern();
}

public static class IsrcErrors
{
    public static readonly Error Empty = Error.Validation("Isrc", "ISRC is required.");

    public static readonly Error InvalidFormat = Error.Validation(
        "Isrc",
        "ISRC must follow the CC-XXX-YY-NNNNN format, e.g. 'EG-TKW-24-00001'.");
}
