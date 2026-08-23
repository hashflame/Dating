using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary>Обрабатывает <see cref="GetPhotosQuery"/> (T-3.1) — фото уже отсортированы по <c>SortOrder</c> репозиторием.</summary>
public sealed class GetPhotosQueryHandler(IPhotoRepository photoRepository)
    : IRequestHandler<GetPhotosQuery, IReadOnlyList<PhotoResult>>
{
    public async Task<IReadOnlyList<PhotoResult>> Handle(GetPhotosQuery request, CancellationToken cancellationToken)
    {
        var photos = await photoRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return photos.Select(UploadPhotoCommandHandler.ToResult).ToList();
    }
}
