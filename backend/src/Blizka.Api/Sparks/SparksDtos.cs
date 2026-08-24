using Blizka.Api.Common;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Sparks;

namespace Blizka.Api.Sparks;

/// <summary>Ответ <c>GET /api/sparks/wallet</c> (T-8.1).</summary>
public sealed record SparksWalletResponse(int Balance, PaginatedResponse<SparkTransactionDto> History, IReadOnlyList<SparkEarnOptionDto> EarnOptions)
{
    public static SparksWalletResponse From(SparksWalletResult result) => new(
        result.Balance,
        new PaginatedResponse<SparkTransactionDto>(result.Items.Select(SparkTransactionDto.From).ToArray(), result.Page, result.PageSize, result.TotalCount),
        result.EarnOptions.Select(SparkEarnOptionDto.From).ToArray());
}

/// <param name="Id">Идентификатор операции.</param>
/// <param name="Amount">Со знаком: положительное — начисление, отрицательное — списание.</param>
/// <param name="Type">Тип операции.</param>
/// <param name="BalanceAfter">Баланс после этой операции.</param>
/// <param name="CreatedAt">Момент операции.</param>
public sealed record SparkTransactionDto(Guid Id, int Amount, SparkTransactionType Type, int BalanceAfter, DateTimeOffset CreatedAt)
{
    public static SparkTransactionDto From(SparkTransactionResult result) => new(
        result.Id, result.Amount, result.Type, result.BalanceAfter, result.CreatedAt);
}

/// <param name="Type">Тип начисления.</param>
/// <param name="Amount">Для <see cref="SparkTransactionType.ProfileCompletion"/> — сумма за один порог (применяется трижды: 60/80/100%).</param>
public sealed record SparkEarnOptionDto(SparkTransactionType Type, int Amount)
{
    public static SparkEarnOptionDto From(SparkEarnOptionResult result) => new(result.Type, result.Amount);
}
