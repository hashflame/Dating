using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Users;
using MediatR;

namespace Blizka.App.UseCases.DatePreferences;

/// <summary>
/// <c>PATCH /api/users/me/date-preferences</c> (T-9.3) — задаёт полный набор предпочтений по формату свидания
/// пользователя (замена, а не добавление/удаление — как <c>InterestIds</c> в <see cref="Interests.PatchUserInterestsCommand"/>).
/// Каталог фиксированный (4 значения из <see cref="DatePreferenceCode"/>), поэтому, в отличие от интересов,
/// создавать новые записи не нужно — только сослаться на уже существующие.
/// </summary>
public sealed record PatchUserDatePreferencesCommand(Guid UserId, IReadOnlyCollection<DatePreferenceCode> Codes, string Locale)
    : IRequest<PatchUserDatePreferencesResult>;

/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
public sealed record PatchUserDatePreferencesResult(
    GetMeResult Profile, int SparksAwarded, IReadOnlyList<DatePreferenceCatalogItemResult> Preferences);
