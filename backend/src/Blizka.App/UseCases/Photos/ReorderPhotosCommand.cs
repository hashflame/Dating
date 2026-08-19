using MediatR;

namespace Blizka.App.UseCases.Photos;

public sealed record ReorderPhotosCommand(Guid UserId, IReadOnlyList<Guid> Order, Guid MainPhotoId)
    : IRequest<IReadOnlyList<PhotoResult>>;
