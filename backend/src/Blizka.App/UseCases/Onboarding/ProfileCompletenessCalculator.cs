using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Данные шагов 1-3 онбординга, слитые в один JSON-объект <see cref="OnboardingDraft.DataJson"/> —
/// разбирается целиком (а не по шагам, как в <see cref="PatchOnboardingDraftCommandHandler"/>), чтобы
/// T-2.3 могло проверить, что все три шага заполнены, и перенести их данные в <see cref="User"/>.
/// </summary>
internal sealed record CombinedOnboardingData(
    string? Name,
    DateOnly? BirthDate,
    Gender? Gender,
    ShowGenderPreference? ShowGender,
    OnboardingAgeRange? AgeRange,
    IReadOnlyCollection<DatingGoal>? DatingGoals,
    Guid? CityId);

/// <summary>Расчёт ProfileCompleteness (T-2.3, decomposition.md): 35% за базовый онбординг + бонусы за необязательные поля профиля.</summary>
internal static class ProfileCompletenessCalculator
{
    public const int BaseCompleteness = 35;
    public const int ThresholdBonusSparks = 2;

    public static readonly IReadOnlyList<int> Thresholds = [60, 80, 100];

    private const int PhotosBonus = 15;
    private const int InterestsBonus = 10;
    private const int PromptsBonus = 10;
    private const int DatePreferencesBonus = 10;
    private const int VerificationBonus = 10;
    private const int VoiceBonus = 5;
    private const int InstagramBonus = 5;

    private const int MinPhotosForBonus = 3;
    private const int MinInterestsForBonus = 5;

    public static int Calculate(User user, int datePreferenceCount)
    {
        var completeness = BaseCompleteness;

        if (user.Photos.Count >= MinPhotosForBonus)
        {
            completeness += PhotosBonus;
        }

        if (user.UserInterests.Count >= MinInterestsForBonus)
        {
            completeness += InterestsBonus;
        }

        if (user.Prompts.Length > 0)
        {
            completeness += PromptsBonus;
        }

        if (datePreferenceCount > 0)
        {
            completeness += DatePreferencesBonus;
        }

        if (user.IsVerified)
        {
            completeness += VerificationBonus;
        }

        if (!string.IsNullOrEmpty(user.VoiceIntroUrl))
        {
            completeness += VoiceBonus;
        }

        if (!string.IsNullOrEmpty(user.InstagramHandle))
        {
            completeness += InstagramBonus;
        }

        return completeness;
    }

    public static NextProfileReward? NextReward(int completeness)
    {
        foreach (var threshold in Thresholds)
        {
            if (completeness < threshold)
            {
                return new NextProfileReward(threshold, ThresholdBonusSparks);
            }
        }

        return null;
    }
}
