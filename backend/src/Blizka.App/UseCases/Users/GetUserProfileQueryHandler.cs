using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Собирает анкету произвольного пользователя по id (ClickUp: «открыть анкету из списка» — списки лайков,
/// T-6.1, отдают только userId/name/age/mainPhotoUrl, полного профиля взять было неоткуда) — по образцу
/// <see cref="GetProfilePreviewQueryHandler"/>, без скоринга совместимости и без полей, доступных только себе.
/// </summary>
public sealed class GetUserProfileQueryHandler(IUserRepository userRepository, IUserBlockRepository userBlockRepository)
    : IRequestHandler<GetUserProfileQuery, UserProfileResult>
{
    public async Task<UserProfileResult> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.TargetUserId, cancellationToken);

        // Удалённый аккаунт (T-16.2, soft delete) не должен быть доступен через прямую ссылку — та же причина,
        // по которой он теперь исключается из списков лайков (LikesRepository).
        if (user is null || user.Status == UserStatus.Deleted)
        {
            throw new UserProfileNotFoundException(request.TargetUserId);
        }

        // T-16.2 — блокировка (в любом направлении) должна скрывать анкету так же, как она уже скрывает цель
        // от свайпов (см. SwipeCommandHandler): иначе блокировка не защищает от просмотра профиля по прямой
        // ссылке/id (баг из e2e-прогона).
        if (await userBlockRepository.ExistsEitherDirectionAsync(request.RequestingUserId, request.TargetUserId, cancellationToken))
        {
            throw new UserProfileNotFoundException(request.TargetUserId);
        }

        var locale = CityLocaleResolver.Resolve(request.Locale);

        var photos = user.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new UserProfilePhotoResult(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain))
            .ToList();

        var interests = user.UserInterests
            .Where(ui => ui.Interest is not null)
            .Select(ui => new UserProfileInterestResult(ui.InterestId, InterestNameResolver.Resolve(ui.Interest!, locale)))
            .ToList();

        var cityName = user.City is null ? string.Empty : CityNameResolver.Resolve(user.City, locale);

        return new UserProfileResult(
            user.Id,
            user.Name,
            AgeCalculator.Calculate(user.BirthDate),
            user.Bio,
            cityName,
            photos,
            interests,
            user.Prompts,
            user.IsVerified,
            user.DatingGoal);
    }
}
