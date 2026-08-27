using Blizka.App.Domain.Entities;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using Blizka.App.UseCases.Onboarding;

namespace Blizka.App.UseCases.Users;

/// <summary>Собирает <see cref="GetMeResult"/> из <see cref="User"/> — общее для <see cref="GetMeQueryHandler"/> и <see cref="PatchUserProfileCommandHandler"/> (T-9.1).</summary>
internal static class UserProfileMapper
{
    /// <param name="locale">Локаль запроса ("ru"/"be"/"en") для <see cref="GetMeResult.CityName"/>/<see cref="GetMeResult.Interests"/> — тот же <c>request.Locale</c>, что и для <paramref name="nextReward"/>, не персистентная <see cref="User.Locale"/>.</param>
    public static GetMeResult ToResult(User user, int completeness, NextProfileReward? nextReward, string locale)
    {
        var resolvedLocale = CityLocaleResolver.Resolve(locale);

        var photos = user.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProfilePreviewPhotoResult(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain))
            .ToList();

        var interests = user.UserInterests
            .Where(ui => ui.Interest is not null)
            .Select(ui => new ProfilePreviewInterestResult(ui.InterestId, InterestNameResolver.Resolve(ui.Interest!, resolvedLocale)))
            .ToList();

        var cityName = user.City is null ? string.Empty : CityNameResolver.Resolve(user.City, resolvedLocale);

        return new GetMeResult(
            user.Id,
            user.TelegramId,
            user.Name,
            AgeCalculator.Calculate(user.BirthDate),
            user.Gender,
            user.BirthDate,
            user.CityId,
            cityName,
            user.Bio,
            user.Height,
            user.Smoking,
            user.Drinking,
            user.Chronotype,
            user.Prompts,
            user.DatingGoal,
            user.IsVerified,
            user.InstagramHandle,
            user.VoiceIntroUrl,
            photos,
            interests,
            user.SparksBalance,
            user.Status,
            user.Locale,
            completeness,
            nextReward);
    }
}
