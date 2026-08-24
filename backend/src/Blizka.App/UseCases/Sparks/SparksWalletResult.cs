using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Sparks;

/// <summary>Результат <c>GET /api/sparks/wallet</c> (T-8.1) — баланс, страница истории и каталог способов заработать.</summary>
public sealed record SparksWalletResult(
    int Balance,
    IReadOnlyList<SparkTransactionResult> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<SparkEarnOptionResult> EarnOptions);

public sealed record SparkTransactionResult(Guid Id, int Amount, SparkTransactionType Type, int BalanceAfter, DateTimeOffset CreatedAt);

/// <summary>
/// Один способ заработать зорки — статический каталог из <c>SparksOptions</c> (T-8.1), без персонализированных
/// флагов «уже получено»: для порогов ProfileCompleteness <see cref="Amount"/> — сумма за один порог
/// (в decomposition.md записано как «2+2+2», применяется трижды — за 60/80/100%).
/// </summary>
public sealed record SparkEarnOptionResult(SparkTransactionType Type, int Amount);
