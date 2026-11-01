using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TrackManagement.Application.Common.Interfaces;
using TrackManagement.Domain.Artists;

namespace TrackManagement.Application.Artists.Commands.CreateArtist;

public sealed record CreateArtistCommand(string Name, string Email, string Country) : IRequest<ErrorOr<ArtistResponse>>;

public sealed class CreateArtistCommandValidator : AbstractValidator<CreateArtistCommand>
{
    public CreateArtistCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Artist.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Artist.EmailMaxLength)
            .EmailAddress();

        RuleFor(x => x.Country)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Country must be an ISO 3166-1 alpha-2 code, e.g. 'EG', 'SA', 'AE'.");
    }
}

public sealed class CreateArtistCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateArtistCommand, ErrorOr<ArtistResponse>>
{
    public async Task<ErrorOr<ArtistResponse>> Handle(CreateArtistCommand request, CancellationToken cancellationToken)
    {
        var email = Artist.NormalizeEmail(request.Email);

        if (await db.Artists.AnyAsync(a => a.Email == email, cancellationToken))
        {
            return ArtistErrors.EmailAlreadyExists(email);
        }

        var artist = Artist.Create(request.Name, request.Email, request.Country);

        db.Artists.Add(artist);
        await db.SaveChangesAsync(cancellationToken);

        return artist.ToResponse();
    }
}
