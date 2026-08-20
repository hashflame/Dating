namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>Swipe</c> конфликтует с уникальным
/// (частичным, см. <c>SwipeConfiguration</c>) индексом на <c>(FromUserId, ToUserId)</c> — т.е. два
/// параллельных запроса свайпа одной и той же пары (например, двойной тап) прошли предварительную
/// проверку "уже свайпнуто" одновременно, до того как один из них закоммитился.
/// Предназначено для внутренней переинтерпретации в вызывающем коде, а не для показа клиенту.
/// </summary>
public sealed class ConcurrentSwipeCreationException(Guid fromUserId, Guid toUserId, Exception innerException)
    : Exception($"Swipe from {fromUserId} to {toUserId} was created concurrently.", innerException)
{
    public Guid FromUserId { get; } = fromUserId;

    public Guid ToUserId { get; } = toUserId;
}
