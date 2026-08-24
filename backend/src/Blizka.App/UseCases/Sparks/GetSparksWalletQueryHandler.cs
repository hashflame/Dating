using Blizka.App.Domain.Enums;
using Blizka.App.Sparks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Sparks;

/// <summary>Обрабатывает <see cref="GetSparksWalletQuery"/> (T-8.1): баланс, история операций, каталог начислений.</summary>
public sealed class GetSparksWalletQueryHandler(
    ISparksService sparksService, IOptions<SparksOptions> sparksOptions, IValidator<GetSparksWalletQuery> validator)
    : IRequestHandler<GetSparksWalletQuery, SparksWalletResult>
{
    public async Task<SparksWalletResult> Handle(GetSparksWalletQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var balance = await sparksService.GetBalanceAsync(request.UserId, cancellationToken);
        var (transactions, totalCount) = await sparksService.GetHistoryAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var items = transactions
            .Select(t => new SparkTransactionResult(t.Id, t.Amount, t.Type, t.BalanceAfter, t.CreatedAt))
            .ToList();

        return new SparksWalletResult(balance, items, totalCount, request.Page, request.PageSize, BuildEarnOptions(sparksOptions.Value));
    }

    private static IReadOnlyList<SparkEarnOptionResult> BuildEarnOptions(SparksOptions options) =>
    [
        new(SparkTransactionType.RegistrationBonus, options.RegistrationBonusAmount),
        new(SparkTransactionType.ProfileCompletion, options.ProfileCompletionThresholdBonusAmount),
        new(SparkTransactionType.Verification, options.VerificationBonusAmount),
        new(SparkTransactionType.Referral, options.ReferralBonusAmount),
        new(SparkTransactionType.IdeaSubmission, options.IdeaSubmissionBonusAmount),
        new(SparkTransactionType.IdeaImplemented, options.IdeaImplementedBonusAmount),
    ];
}
