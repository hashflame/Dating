using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.Photos;
using Blizka.App.Sparks;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Likes;

/// <summary>
/// Обрабатывает <see cref="GetIncomingLikesQuery"/> (T-6.1). Пока <c>User.LikesRevealed</c> не выставлен,
/// возвращает только количество и заблюренное превью (до <see cref="PreviewSize"/> фото) — блюр генерируется
/// на лету (<see cref="PhotoImageProcessor.Blur"/>) по уже сохранённому thumbnail, не кэшируется и не хранится
/// отдельным вариантом фото (согласовано с пользователем: своей инфраструктуры блюра в T-3.1 не было).
/// После разблокировки отдаёт полный список без блюра.
/// </summary>
public sealed class GetIncomingLikesQueryHandler(
    IUserRepository userRepository,
    ILikesRepository likesRepository,
    IPhotoStorageService photoStorageService,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<GetIncomingLikesQuery, IncomingLikesResult>
{
    private const int PreviewSize = 4; // spec.md 7.1 — пример ответа показывает ровно четыре blurredPhotoUrl.

    public async Task<IncomingLikesResult> Handle(GetIncomingLikesQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (user.LikesRevealed)
        {
            var entries = await likesRepository.GetIncomingAsync(request.UserId, cancellationToken);
            return new IncomingLikesResult(
                entries.Count, Revealed: true, sparksOptions.Value.LikesRevealCost, [], entries.Select(LikeResultMapper.ToUserResult).ToList());
        }

        var count = await likesRepository.CountIncomingAsync(request.UserId, cancellationToken);
        var preview = await likesRepository.GetIncomingPreviewAsync(request.UserId, PreviewSize, cancellationToken);
        var blurredPhotos = await BlurMainPhotosAsync(preview, cancellationToken);

        return new IncomingLikesResult(count, Revealed: false, sparksOptions.Value.LikesRevealCost, blurredPhotos, []);
    }

    private async Task<IReadOnlyList<byte[]>> BlurMainPhotosAsync(IReadOnlyList<LikeEntry> entries, CancellationToken cancellationToken)
    {
        var blurred = new List<byte[]>(entries.Count);

        foreach (var entry in entries)
        {
            var mainPhoto = entry.User.Photos.FirstOrDefault(p => p.IsMain)
                ?? entry.User.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();
            if (mainPhoto is null)
            {
                continue;
            }

            var key = PhotoStorageKeys.Thumbnail(PhotoStorageKeys.Prefix(mainPhoto.UserId, mainPhoto.Id));
            try
            {
                var thumbnailBytes = await photoStorageService.DownloadAsync(key, cancellationToken);
                blurred.Add(PhotoImageProcessor.Blur(thumbnailBytes));
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Один недоступный/повреждённый thumbnail (несогласованность с хранилищем, гонка с удалением
                // фото между чтением списка и генерацией превью) не должен обрушивать весь список 500-й —
                // пропускаем эту запись, как и при отсутствии главного фото выше.
            }
        }

        return blurred;
    }
}
