using MediatR;

namespace Blizka.App.UseCases.Photos;

public sealed record ImportTelegramPhotoCommand(Guid UserId, string PhotoUrl) : IRequest<PhotoResult>;
