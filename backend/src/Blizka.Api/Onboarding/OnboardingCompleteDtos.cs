namespace Blizka.Api.Onboarding;

/// <summary>Результат <c>POST /api/onboarding/complete</c>.</summary>
/// <param name="SparksAwarded">Сколько зорок начислено этим вызовом (регистрационный бонус + бонусы за достигнутые пороги ProfileCompleteness).</param>
/// <param name="ProfileCompleteness">Итоговая заполненность профиля в процентах.</param>
/// <param name="NextReward">Ближайший недостигнутый порог заполненности и награда за него; <c>null</c>, если профиль уже заполнен на 100%.</param>
public sealed record OnboardingCompleteResponse(int SparksAwarded, int ProfileCompleteness, NextRewardResponse? NextReward);

/// <param name="Threshold">Порог ProfileCompleteness (60, 80 или 100).</param>
/// <param name="SparksReward">Сколько зорок начислится за его достижение.</param>
public sealed record NextRewardResponse(int Threshold, int SparksReward);
