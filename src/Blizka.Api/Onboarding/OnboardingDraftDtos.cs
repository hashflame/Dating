using System.Text.Json;

namespace Blizka.Api.Onboarding;

/// <summary>Тело запроса <c>PATCH /api/onboarding/draft</c>.</summary>
/// <param name="Step">Номер шага онбординга (1-3; шаг 4 — фото — обрабатывается отдельным эндпоинтом T-3.1).</param>
/// <param name="Data">Данные шага; форма зависит от <paramref name="Step"/>, см. decomposition.md T-2.1.</param>
public sealed record PatchOnboardingDraftRequest(int Step, JsonElement Data);

/// <summary>Текущее состояние черновика онбординга пользователя.</summary>
/// <param name="Step">Номер последнего сохранённого шага (0, если черновик ещё не создан).</param>
/// <param name="Data">Накопленные данные всех сохранённых шагов, слитые в один объект.</param>
public sealed record OnboardingDraftResponse(int Step, JsonElement Data);
