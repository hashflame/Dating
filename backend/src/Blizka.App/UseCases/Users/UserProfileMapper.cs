using Blizka.App.Domain.Entities;
using Blizka.App.UseCases.Onboarding;

namespace Blizka.App.UseCases.Users;

/// <summary>Собирает <see cref="GetMeResult"/> из <see cref="User"/> — общее для <see cref="GetMeQueryHandler"/> и <see cref="PatchUserProfileCommandHandler"/> (T-9.1).</summary>
internal static class UserProfileMapper
{
    public static GetMeResult ToResult(User user, int completeness, NextProfileReward? nextReward) => new(
        user.Id,
        user.TelegramId,
        user.Name,
        user.Gender,
        user.BirthDate,
        user.CityId,
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
        user.SparksBalance,
        user.Status,
        user.Locale,
        completeness,
        nextReward);
}
