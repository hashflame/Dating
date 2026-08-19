using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>Шаг 1 (S-03): базовые данные о себе.</summary>
public sealed record OnboardingStep1Data(string Name, DateOnly BirthDate, Gender Gender);

/// <summary>Шаг 2 (S-04): кого искать.</summary>
public sealed record OnboardingStep2Data(
    ShowGenderPreference ShowGender,
    OnboardingAgeRange AgeRange,
    IReadOnlyCollection<DatingGoal> DatingGoals);

public sealed record OnboardingAgeRange(int Min, int Max);

/// <summary>Шаг 3 (S-05): город.</summary>
public sealed record OnboardingStep3Data(Guid CityId);
