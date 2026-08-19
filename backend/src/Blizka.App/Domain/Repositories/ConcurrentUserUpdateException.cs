namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда обновление уже существующего <c>User</c> конфликтует с его
/// актуальной версией в БД (проверяется по xmin) — т.е. запись успела измениться между чтением и
/// <c>SaveChangesAsync</c> (например, два параллельных <c>POST /api/onboarding/complete</c> для одного
/// и того же пользователя).
/// Предназначено для внутренней переинтерпретации в вызывающем коде, а не для показа клиенту.
/// </summary>
public sealed class ConcurrentUserUpdateException(Guid userId, Exception innerException)
    : Exception($"User {userId} was updated concurrently.", innerException)
{
    public Guid UserId { get; } = userId;
}
