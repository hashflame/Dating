namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового кастомного <c>Interest</c> конфликтует с уникальным
/// индексом по <c>NameRu</c> — т.е. интерес с таким названием уже был создан параллельным запросом между
/// <see cref="IInterestRepository.FindByNameAsync"/> и <c>SaveChangesAsync</c> (T-9.2).
/// Предназначено для внутренней переинтерпретации в вызывающем коде, а не для показа клиенту.
/// </summary>
public sealed class ConcurrentInterestCreationException(string name, Exception innerException)
    : Exception($"Interest with name '{name}' was created concurrently.", innerException)
{
    public string Name { get; } = name;
}
