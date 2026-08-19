using Blizka.App.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary>Переупорядочивает фото пользователя и назначает главное (T-3.1, <c>PATCH /api/users/me/photos/reorder</c>).</summary>
public sealed class ReorderPhotosCommandHandler(IPhotoRepository photoRepository)
    : IRequestHandler<ReorderPhotosCommand, IReadOnlyList<PhotoResult>>
{
    public async Task<IReadOnlyList<PhotoResult>> Handle(ReorderPhotosCommand request, CancellationToken cancellationToken)
    {
        var photos = await photoRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var photoById = photos.ToDictionary(p => p.Id);

        if (request.Order.Count != photos.Count ||
            request.Order.Distinct().Count() != request.Order.Count ||
            !request.Order.All(photoById.ContainsKey))
        {
            throw new ValidationException(
                [new ValidationFailure("order", "order должен содержать ровно все id фото пользователя без повторов.")]);
        }

        if (!photoById.ContainsKey(request.MainPhotoId))
        {
            throw new ValidationException(
                [new ValidationFailure("mainPhotoId", "mainPhotoId должен быть одним из id в order.")]);
        }

        for (var index = 0; index < request.Order.Count; index++)
        {
            var photo = photoById[request.Order[index]];
            photo.SortOrder = index;
            photo.IsMain = photo.Id == request.MainPhotoId;
        }

        await photoRepository.SaveChangesAsync(cancellationToken);

        return request.Order.Select(id => UploadPhotoCommandHandler.ToResult(photoById[id])).ToList();
    }
}
