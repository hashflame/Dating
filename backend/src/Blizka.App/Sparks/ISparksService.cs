using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Sparks;

/// <summary>
/// Минимальный кошелёк зорок (T-5.2 берёт на себя только то, что нужно для суперлайка — полный
/// кошелёк с <c>Award</c>/<c>GetBalance</c>/<c>GetHistory</c> и <c>/api/sparks/wallet</c> — задача T-8.1,
/// пока не реализована; она достроит этот интерфейс, а не заменит).
/// </summary>
public interface ISparksService
{
    /// <summary>
    /// Списывает <paramref name="amount"/> зорок с уже загруженного и отслеживаемого контекстом
    /// <paramref name="user"/> и ставит в очередь запись <c>SparkTransaction</c> — сохранение (и его
    /// транзакционные гарантии вместе с остальными изменениями того же запроса) на совести вызывающего кода.
    /// </summary>
    /// <exception cref="Domain.Exceptions.InsufficientSparksException">Баланс пользователя меньше <paramref name="amount"/>.</exception>
    Task SpendAsync(User user, int amount, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken);

    /// <summary>
    /// Начисляет <paramref name="amount"/> зорок обратно на баланс уже загруженного и отслеживаемого
    /// контекстом <paramref name="user"/> и ставит в очередь запись <c>SparkTransaction</c> с
    /// <see cref="SparkTransactionType.Refund"/> — например, возврат за отменённый суперлайк (T-5.3).
    /// </summary>
    Task RefundAsync(User user, int amount, Guid referenceId, CancellationToken cancellationToken);
}
