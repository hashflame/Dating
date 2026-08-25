using Blizka.App.UseCases.Users;
using MediatR;

namespace Blizka.App.UseCases.Interests;

/// <summary>
/// <c>PATCH /api/users/me/interests</c> (T-9.2) — задаёт полный набор интересов пользователя (замена, а не
/// добавление/удаление — как <c>Prompts</c> в <see cref="PatchUserProfileCommand"/>). <c>InterestIds</c> —
/// уже существующие в каталоге интересы; <c>CustomInterests</c> — названия новых кастомных интересов
/// (decomposition.md описывает контракт как один плоский <c>{ interestIds: [...] }</c>, но также требует
/// уметь создавать кастомный интерес "если interestId не найден в каталоге и isCustom: true" — без готового
/// id создать такую запись с одним лишь <c>interestIds</c> невозможно, поэтому названия новых кастомных
/// интересов вынесены в отдельное поле; см. "Что сделано" T-9.2).
/// </summary>
public sealed record PatchUserInterestsCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> InterestIds,
    IReadOnlyCollection<string> CustomInterests,
    string Locale) : IRequest<PatchUserInterestsResult>;

/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
public sealed record PatchUserInterestsResult(GetMeResult Profile, int SparksAwarded, IReadOnlyList<InterestCatalogItemResult> Interests);
