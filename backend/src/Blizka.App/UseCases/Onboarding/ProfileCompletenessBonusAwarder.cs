using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Sparks;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Начисляет бонусные зорки за впервые достигнутые пороги ProfileCompleteness (60/80/100%, T-2.3) —
/// общая логика для <c>CompleteOnboardingCommandHandler</c> (после завершения онбординга) и
/// <c>PatchUserProfileCommandHandler</c> (T-9.1, после редактирования профиля): в обоих случаях порог
/// может быть достигнут впервые, и защита от повторного начисления (<c>CompletenessBonus60/80/100AwardedAt</c>)
/// должна работать одинаково.
/// </summary>
internal static class ProfileCompletenessBonusAwarder
{
    public static async Task<int> AwardAsync(
        User user, ISparksService sparksService, int bonusAmount, CancellationToken cancellationToken)
    {
        var totalAwarded = 0;
        var now = DateTimeOffset.UtcNow;

        if (user.ProfileCompleteness >= 60 && user.CompletenessBonus60AwardedAt is null)
        {
            user.CompletenessBonus60AwardedAt = now;
            await sparksService.AwardAsync(user, bonusAmount, SparkTransactionType.ProfileCompletion, referenceId: null, cancellationToken);
            totalAwarded += bonusAmount;
        }

        if (user.ProfileCompleteness >= 80 && user.CompletenessBonus80AwardedAt is null)
        {
            user.CompletenessBonus80AwardedAt = now;
            await sparksService.AwardAsync(user, bonusAmount, SparkTransactionType.ProfileCompletion, referenceId: null, cancellationToken);
            totalAwarded += bonusAmount;
        }

        if (user.ProfileCompleteness >= 100 && user.CompletenessBonus100AwardedAt is null)
        {
            user.CompletenessBonus100AwardedAt = now;
            await sparksService.AwardAsync(user, bonusAmount, SparkTransactionType.ProfileCompletion, referenceId: null, cancellationToken);
            totalAwarded += bonusAmount;
        }

        return totalAwarded;
    }
}
