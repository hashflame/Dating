namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда создание нового кастомного интереса конфликтует с уникальным индексом по названию —
/// параллельный запрос успел создать интерес с тем же названием первым (T-9.2, см.
/// <see cref="Repositories.ConcurrentInterestCreationException"/>). Сам интерес уже существует в каталоге
/// под тем же названием, клиенту стоит просто повторить запрос.
/// </summary>
public sealed class InterestCreationConflictException(string name, Exception innerException)
    : BlizkaDomainException(
        "INTEREST_CREATION_CONFLICT",
        $"Interest with name '{name}' was created concurrently by another request.",
        new Dictionary<string, object?> { ["name"] = name },
        innerException)
{
    public string Name { get; } = name;
}
