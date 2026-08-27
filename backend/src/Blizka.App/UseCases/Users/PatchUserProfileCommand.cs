using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Onboarding;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// <c>PATCH /api/users/me/profile</c> (T-9.1) — частичное обновление: поле со значением <c>null</c>
/// оставляет уже сохранённое значение без изменений (тот же принцип, что и <c>PatchFeedFiltersCommand</c>,
/// T-5.4). Ровно набор полей из decomposition.md — <c>name, bio, height, smoking, drinking, chronotype,
/// prompts, datingGoals</c>; город/пол/дата рождения сюда не входят (переносятся один раз при завершении
/// онбординга, T-2.3), а Instagram/голосовое приветствие — предмет отдельных будущих задач.
/// </summary>
/// <param name="DatingGoals">До двух целей (как в макете S-04). Раньше поле было одиночным (<c>DatingGoal?</c>),
/// и вторая выбранная на онбординге цель молча терялась из анкеты (тикет ClickUp).</param>
/// <param name="Locale">См. <see cref="GetMeQuery.Locale"/> — та же локаль для <see cref="NextProfileReward.Hint"/>.</param>
public sealed record PatchUserProfileCommand(
    Guid UserId,
    string? Name,
    string? Bio,
    int? Height,
    SmokingHabit? Smoking,
    DrinkingHabit? Drinking,
    Chronotype? Chronotype,
    IReadOnlyCollection<string>? Prompts,
    IReadOnlyCollection<DatingGoal>? DatingGoals,
    string Locale) : IRequest<PatchUserProfileResult>;

/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
public sealed record PatchUserProfileResult(GetMeResult Profile, int SparksAwarded);
