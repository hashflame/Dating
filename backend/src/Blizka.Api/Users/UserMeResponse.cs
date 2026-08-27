using Blizka.Api.Onboarding;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Users;

namespace Blizka.Api.Users;

/// <summary>Полный профиль текущего пользователя (T-9.1) — базовые данные, редактируемые поля, баланс зорок и заполненность.</summary>
/// <param name="Id">Id пользователя.</param>
/// <param name="TelegramId">Telegram id пользователя.</param>
/// <param name="Name">Имя.</param>
/// <param name="Age">Возраст, посчитан на сервере из <see cref="BirthDate"/> (баг T-9.1: раньше фронту приходилось считать самому, что расходилось с лентой на границе дня рождения).
/// <c>null</c>, пока <see cref="BirthDate"/> не задан реально (шаг 1 онбординга ещё не пройден).</param>
/// <param name="Gender">Пол.</param>
/// <param name="BirthDate">Дата рождения.</param>
/// <param name="CityId">Id города из онбординга; сам профиль (T-9.1) его не меняет.</param>
/// <param name="CityName">Локализованное название города; пустая строка, пока город не выбран.</param>
/// <param name="Bio">О себе.</param>
/// <param name="Height">Рост, см.</param>
/// <param name="Smoking">Отношение к курению.</param>
/// <param name="Drinking">Отношение к алкоголю.</param>
/// <param name="Chronotype">Хронотип.</param>
/// <param name="Prompts">Промпты профиля (до 3 штук).</param>
/// <param name="DatingGoal">Основная цель знакомства.</param>
/// <param name="IsVerified">Пройдена ли верификация по селфи (T-18.1).</param>
/// <param name="InstagramHandle">Привязанный Instagram, если есть.</param>
/// <param name="VoiceIntroUrl">Голосовое приветствие, если загружено.</param>
/// <param name="Photos">Фото профиля — тот же формат, что и в <c>GET /api/users/me/preview</c> (баг T-9.1: раньше требовался отдельный запрос).</param>
/// <param name="Interests">Интересы профиля — тот же формат, что и в <c>GET /api/users/me/preview</c> (баг T-9.1: раньше требовался отдельный запрос).</param>
/// <param name="SparksBalance">Текущий баланс зорок.</param>
/// <param name="Status">Статус аккаунта — по нему клиент определяет, завершён ли онбординг (<c>New</c>/<c>Onboarding</c> — не завершён).</param>
/// <param name="Locale">Локаль пользователя.</param>
/// <param name="ProfileCompleteness">Заполненность профиля в процентах.</param>
/// <param name="NextReward">Ближайший недостигнутый порог заполненности и награда за него; <c>null</c>, если профиль уже заполнен на 100%.</param>
public sealed record UserMeResponse(
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
    string[] Prompts,
    DatingGoal? DatingGoal,
    bool IsVerified,
    string? InstagramHandle,
    string? VoiceIntroUrl,
    ProfilePreviewPhotoDto[] Photos,
    ProfilePreviewInterestDto[] Interests,
    int SparksBalance,
    UserStatus Status,
    string Locale,
    int ProfileCompleteness,
    NextRewardResponse? NextReward)
{
    public static UserMeResponse From(GetMeResult result) => new(
        result.Id,
        result.TelegramId,
        result.Name,
        result.Age,
        result.Gender,
        result.BirthDate,
        result.CityId,
        result.CityName,
        result.Bio,
        result.Height,
        result.Smoking,
        result.Drinking,
        result.Chronotype,
        [.. result.Prompts],
        result.DatingGoal,
        result.IsVerified,
        result.InstagramHandle,
        result.VoiceIntroUrl,
        result.Photos.Select(p => new ProfilePreviewPhotoDto(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain)).ToArray(),
        result.Interests.Select(i => new ProfilePreviewInterestDto(i.Id, i.Name)).ToArray(),
        result.SparksBalance,
        result.Status,
        result.Locale,
        result.ProfileCompleteness,
        result.NextReward is { } reward ? new NextRewardResponse(reward.Threshold, reward.SparksReward, reward.Hint) : null);
}
