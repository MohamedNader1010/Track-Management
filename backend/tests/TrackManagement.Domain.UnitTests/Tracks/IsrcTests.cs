using TrackManagement.Domain.Tracks;

namespace TrackManagement.Domain.UnitTests.Tracks;

public sealed class IsrcTests
{
    [Theory]
    [InlineData("EG-TKW-24-00001", "EGTKW2400001")]
    [InlineData("egtkw2400001", "EGTKW2400001")]
    [InlineData("  US-RC1-76-07839  ", "USRC17607839")]
    public void Create_normalizes_valid_codes(string raw, string expected)
    {
        var result = Isrc.Create(raw);

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EG-TKW-24-0001")]     // too short
    [InlineData("EG-TKW-24-000001")]   // too long
    [InlineData("12-TKW-24-00001")]    // country must be letters
    [InlineData("EG-TKW-2A-00001")]    // year must be digits
    [InlineData("EG_TKW_24_00001")]    // wrong separator
    public void Create_rejects_invalid_codes(string? raw)
    {
        var result = Isrc.Create(raw);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Isrc");
    }

    [Fact]
    public void Isrcs_with_same_value_are_equal()
    {
        Isrc.Create("EG-TKW-24-00001").Value.ShouldBe(Isrc.Create("egtkw2400001").Value);
    }

    [Fact]
    public void ToDisplayString_formats_with_hyphens()
    {
        Isrc.Create("EGTKW2400001").Value.ToDisplayString().ShouldBe("EG-TKW-24-00001");
    }
}
