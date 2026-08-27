using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Onboarding;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <param name="Locale">
/// Локаль текущего запроса ("ru"/"be"/"en"), которой локализуется <see cref="NextProfileReward.Hint"/> —
/// резолвится в Api-слое тем же <c>RequestLocaleResolver</c>, что и <c>CompleteOnboardingCommand.Locale</c>
/// (см. его doc-комментарий), а не берётся из персистентной <see cref="Domain.Entities.User.Locale"/>.
/// </param>
public sealed record GetMeQuery(Guid UserId, string Locale) : IRequest<GetMeResult>;

/// <summary>
/// Полный профиль текущего пользователя (T-9.1): базовые данные, редактируемые поля профиля, баланс
/// зорок, заполненность и ближайшая награда за неё. До T-9.1 здесь был урезанный набор полей
/// (id/telegramId/name/sparksBalance/status/locale) — расширено на месте, как предписано заметкой T-9.1
/// в decomposition.md, а не задублировано вторым эндпоинтом.
/// </summary>
/// <param name="Age">Посчитан на сервере из <see cref="BirthDate"/> — тем же способом, что и в ленте/мэтчах/превью
/// профиля, чтобы фронт не считал возраст на клиенте и не расходился с ними на границе дня рождения (баг T-9.1).
/// <c>null</c>, пока <see cref="BirthDate"/> не задан реально (шаг 1 онбординга ещё не пройден) — из e2e-прогона:
/// раньше это давало бессмысленный возраст вроде "2025 лет".</param>
/// <param name="CityName">Локализованное название <see cref="CityId"/>; пустая строка, пока город не выбран (до онбординга).</param>
/// <param name="Photos">Фото профиля — тот же набор, что и в <c>GET /api/users/me/preview</c>, чтобы не требовать отдельного запроса.</param>
/// <param name="Interests">Интересы профиля — тот же набор, что и в <c>GET /api/users/me/preview</c>, чтобы не требовать отдельного запроса.</param>
public sealed record GetMeResult(
    Guid Id,
    long TelegramId,
    string Name,
    int? Age,
    Gender Gender,
    DateOnly BirthDate,
    Guid? CityId,
    string CityName,
    string? Bio,
    int? Height,
    SmokingHabit? Smoking,
    DrinkingHabit? Drinking,
    Chronotype? Chronotype,
    IReadOnlyList<string> Prompts,
    IReadOnlyList<DatingGoal> DatingGoals,
    bool IsVerified,
    string? InstagramHandle,
    string? VoiceIntroUrl,
    IReadOnlyList<ProfilePreviewPhotoResult> Photos,
    IReadOnlyList<ProfilePreviewInterestResult> Interests,
    int SparksBalance,
    UserStatus Status,
    string Locale,
    int ProfileCompleteness,
    NextProfileReward? NextReward);
