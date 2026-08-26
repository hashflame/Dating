using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Onboarding;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Sparks;

/// <summary>Обрабатывает <see cref="GetSparksWalletQuery"/> (T-8.1): баланс, история операций, каталог начислений.</summary>
public sealed class GetSparksWalletQueryHandler(
    ISparksService sparksService, IUserRepository userRepository,
    IOptions<SparksOptions> sparksOptions, IValidator<GetSparksWalletQuery> validator)
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

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        return new SparksWalletResult(
            balance, items, totalCount, request.Page, request.PageSize, BuildEarnOptions(sparksOptions.Value, user, request.Locale));
    }

    // earnOptions отдавал только { type, amount } — фронту нечем было показать «до 80% осталось» или «уже
    // получено» (баг T-8.1). RegistrationBonus/ProfileCompletion/Verification уже имеют данные на User для
    // честного прогресса; у Referral/IdeaSubmission/IdeaImplemented вызывающего кода ещё нет (T-20.1/T-19.1) —
    // progress/threshold/usedThisMonth для них остаются null, а не выдумываются.
    private static IReadOnlyList<SparkEarnOptionResult> BuildEarnOptions(SparksOptions options, User user, string locale)
    {
        var nextThreshold = ProfileCompletenessCalculator.Thresholds.FirstOrDefault(t => user.ProfileCompleteness < t);
        var allThresholdsAwarded = user.CompletenessBonus60AwardedAt is not null
            && user.CompletenessBonus80AwardedAt is not null
            && user.CompletenessBonus100AwardedAt is not null;

        return
        [
            Build(SparkTransactionType.RegistrationBonus, options.RegistrationBonusAmount, locale,
                progress: user.RegistrationBonusAwardedAt is null ? 0 : 1, threshold: 1,
                completed: user.RegistrationBonusAwardedAt is not null),
            Build(SparkTransactionType.ProfileCompletion, options.ProfileCompletionThresholdBonusAmount, locale,
                progress: user.ProfileCompleteness, threshold: nextThreshold == 0 ? 100 : nextThreshold,
                completed: allThresholdsAwarded),
            Build(SparkTransactionType.Verification, options.VerificationBonusAmount, locale,
                progress: user.IsVerified ? 1 : 0, threshold: 1, completed: user.IsVerified),
            Build(SparkTransactionType.Referral, options.ReferralBonusAmount, locale,
                progress: null, threshold: null, completed: false),
            Build(SparkTransactionType.IdeaSubmission, options.IdeaSubmissionBonusAmount, locale,
                progress: null, threshold: null, completed: false),
            Build(SparkTransactionType.IdeaImplemented, options.IdeaImplementedBonusAmount, locale,
                progress: null, threshold: null, completed: false),
        ];
    }

    private static SparkEarnOptionResult Build(
        SparkTransactionType type, int amount, string locale, int? progress, int? threshold, bool completed) => new(
        type, amount, SparkEarnOptionLabelCatalog.Resolve(type, locale), progress, threshold, completed, UsedThisMonth: null);
}
