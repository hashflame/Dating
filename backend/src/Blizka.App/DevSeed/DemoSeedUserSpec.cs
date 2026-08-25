using Blizka.App.Domain.Enums;

namespace Blizka.App.DevSeed;

/// <summary>
/// Фиксированная анкета одного из 10 демо-пользователей (спека 003, docs/specs/003-demo-seed-data.md) —
/// единый источник истины и для сидирования (<c>Blizka.Data.DevSeed.DemoSeedService</c>), и для dev-логина
/// в обход Telegram (<c>Blizka.Api.Auth.TelegramAuthMiddleware</c>), чтобы имя/username не расходились
/// между тем, что засеяно в БД, и тем, что middleware подставляет в синтетический <c>TelegramInitData</c>.
/// </summary>
public sealed record DemoSeedUserSpec(
    int Index,
    long TelegramId,
    string Username,
    string FirstName,
    string LastName,
    Gender Gender,
    DateOnly BirthDate,
    DatingGoal DatingGoal,
    string Bio,
    int PhotoCount,
    IReadOnlyList<int> InterestIndexes)
{
    public string FullName => $"{FirstName} {LastName}";
}
