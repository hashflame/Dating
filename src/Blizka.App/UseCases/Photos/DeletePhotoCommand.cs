using MediatR;

namespace Blizka.App.UseCases.Photos;

public sealed record DeletePhotoCommand(Guid UserId, Guid PhotoId) : IRequest;
