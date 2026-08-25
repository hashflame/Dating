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
public sealed record GetMeResult(
    Guid Id,
    long TelegramId,
    string Name,
    Gender Gender,
    DateOnly BirthDate,
    Guid? CityId,
    string? Bio,
    int? Height,
    SmokingHabit? Smoking,
    DrinkingHabit? Drinking,
    Chronotype? Chronotype,
    IReadOnlyList<string> Prompts,
    DatingGoal? DatingGoal,
    bool IsVerified,
    string? InstagramHandle,
    string? VoiceIntroUrl,
    int SparksBalance,
    UserStatus Status,
    string Locale,
    int ProfileCompleteness,
    NextProfileReward? NextReward);
