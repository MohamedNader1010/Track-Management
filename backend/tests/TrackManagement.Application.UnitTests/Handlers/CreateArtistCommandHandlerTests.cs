using ErrorOr;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Artists.Commands.CreateArtist;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Application.UnitTests.Handlers;

public sealed class CreateArtistCommandHandlerTests : IDisposable
{
    private readonly AppDbContext _db = new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Creates_artist_with_normalized_email_and_country()
    {
        var handler = new CreateArtistCommandHandler(_db);

        var result = await handler.Handle(new CreateArtistCommand(" Nour ", " Nour@Example.COM ", "eg"), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("Nour");
        result.Value.Email.ShouldBe("nour@example.com");
        result.Value.Country.ShouldBe("EG");
        (await _db.Artists.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Rejects_duplicate_email_regardless_of_casing()
    {
        var handler = new CreateArtistCommandHandler(_db);
        await handler.Handle(new CreateArtistCommand("Nour", "nour@example.com", "EG"), CancellationToken.None);

        var result = await handler.Handle(new CreateArtistCommand("Other", "NOUR@example.com", "SA"), CancellationToken.None);

        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Artist.EmailAlreadyExists");
    }
}
