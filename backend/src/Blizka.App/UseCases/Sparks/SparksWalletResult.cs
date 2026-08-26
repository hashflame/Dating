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
/// Один способ заработать зорки (T-8.1). Для порогов ProfileCompleteness <see cref="Amount"/> — сумма
/// за один порог (в decomposition.md записано как «2+2+2», применяется трижды — за 60/80/100%).
/// </summary>
/// <param name="Label">Локализованное название способа заработка (баг T-8.1: раньше фронту нечем было подписать вариант).</param>
/// <param name="Progress">Текущий прогресс к <see cref="Threshold"/> (например, ProfileCompleteness в процентах); <c>null</c>, если понятие прогресса не применимо к этому типу или фича ещё не реализована (Referral/IdeaSubmission/IdeaImplemented — T-20.1/T-19.1).</param>
/// <param name="Threshold">Значение <see cref="Progress"/>, при котором начисление срабатывает; <c>null</c> по тем же причинам, что и <see cref="Progress"/>.</param>
/// <param name="Completed">Уже получено (для одноразовых типов — регистрация/верификация) или полностью выбрано (для ProfileCompletion — все три порога); всегда <c>false</c> для ещё не реализованных Referral/IdeaSubmission/IdeaImplemented.</param>
/// <param name="UsedThisMonth">Сколько раз тип уже использован в текущем календарном месяце; <c>null</c>, если у типа нет месячного лимита или лимит есть только в спеке, а начисляющий код ещё не реализован (Referral/IdeaSubmission/IdeaImplemented — T-20.1/T-19.1).</param>
public sealed record SparkEarnOptionResult(
    SparkTransactionType Type, int Amount, string Label, int? Progress, int? Threshold, bool Completed, int? UsedThisMonth);
