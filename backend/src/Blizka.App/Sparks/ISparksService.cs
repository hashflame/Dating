using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Sparks;

/// <summary>Кошелёк зорок (T-8.1): начисления, списания, баланс, история операций.</summary>
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

    /// <summary>
    /// Начисляет <paramref name="amount"/> зорок уже загруженному и отслеживаемому контекстом
    /// <paramref name="user"/> и ставит в очередь запись <c>SparkTransaction</c> с указанным
    /// <paramref name="type"/> — регистрационный бонус, пороги ProfileCompleteness, верификация,
    /// реферал, идеи (T-8.1). Сохранение — на совести вызывающего кода, как и в <see cref="SpendAsync"/>.
    /// </summary>
    Task AwardAsync(User user, int amount, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken);

    /// <summary>
    /// Меняет баланс уже загруженного и отслеживаемого контекстом <paramref name="user"/> на произвольную
    /// <paramref name="delta"/> (может быть отрицательной) и ставит в очередь запись <c>SparkTransaction</c> —
    /// в отличие от <see cref="SpendAsync"/>, не проверяет достаточность баланса. Для служебных корректировок
    /// вне обычных начислений/списаний (сейчас — только <see cref="Domain.Enums.SparkTransactionType.DevReset"/>
    /// в <c>ResetUserStateCommandHandler</c>), чтобы баланс оставался производной от журнала, а не отдельной
    /// правдой (S-46), даже когда меняется напрямую dev-инструментом.
    /// </summary>
    Task AdjustAsync(User user, int delta, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken);

    /// <summary>Текущий баланс зорок пользователя (T-8.1, <c>GET /api/sparks/wallet</c>).</summary>
    Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Страница истории операций пользователя, отсортированная по <c>CreatedAt</c> убыв. (T-8.1).</summary>
    Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);
}
