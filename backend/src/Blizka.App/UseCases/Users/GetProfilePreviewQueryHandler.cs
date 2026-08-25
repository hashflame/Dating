using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Собирает карточку профиля "как видят другие" (T-9.1) — по образцу <see cref="GetFeedQueryHandler"/>, без скоринга совместимости.</summary>
public sealed class GetProfilePreviewQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetProfilePreviewQuery, ProfilePreviewResult>
{
    public async Task<ProfilePreviewResult> Handle(GetProfilePreviewQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var locale = CityLocaleResolver.Resolve(request.Locale);

        var photos = user.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProfilePreviewPhotoResult(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain))
            .ToList();

        var interests = user.UserInterests
            .Where(ui => ui.Interest is not null)
            .Select(ui => new ProfilePreviewInterestResult(ui.InterestId, InterestNameResolver.Resolve(ui.Interest!, locale)))
            .ToList();

        var cityName = user.City is null ? string.Empty : CityNameResolver.Resolve(user.City, locale);

        return new ProfilePreviewResult(
            user.Id,
            user.Name,
            CalculateAge(user.BirthDate),
            user.Bio,
            cityName,
            photos,
            interests,
            user.Prompts,
            user.IsVerified,
            user.DatingGoal);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
