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

        // Два прохода с промежуточным SaveChanges: (UserId, SortOrder) и частичный индекс по IsMain — обычные
        // (не DEFERRABLE) уникальные индексы, Postgres проверяет их на каждом UPDATE, а не на коммите. Если менять
        // позиции/главное фото одним SaveChangesAsync, первый же UPDATE может столкнуться с ещё не обновлённой
        // записью на том же значении → duplicate key (500). Временные отрицательные позиции и IsMain=false
        // гарантированно не конфликтуют ни с текущими, ни с финальными значениями. Обе фазы — в одной транзакции
        // БД (SaveChangesTwoPhaseAsync): без неё сбой между двумя SaveChangesAsync оставил бы фото в переходном
        // состоянии (все SortOrder отрицательные, ни одного IsMain=true) до следующего успешного reorder.
        for (var index = 0; index < request.Order.Count; index++)
        {
            var photo = photoById[request.Order[index]];
            photo.SortOrder = -(index + 1);
            photo.IsMain = false;
        }

        await photoRepository.SaveChangesTwoPhaseAsync(
            () =>
            {
                for (var index = 0; index < request.Order.Count; index++)
                {
                    var photo = photoById[request.Order[index]];
                    photo.SortOrder = index;
                    photo.IsMain = photo.Id == request.MainPhotoId;
                }
            },
            cancellationToken);

        return request.Order.Select(id => UploadPhotoCommandHandler.ToResult(photoById[id])).ToList();
    }
}
