using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Likes;

/// <summary>
/// Обрабатывает <see cref="RevealIncomingLikesCommand"/> (T-6.1): списывает <c>Sparks:LikesRevealCost</c> и
/// выставляет <c>User.LikesRevealed</c> — разблокировка навсегда, не за каждого лайкнувшего отдельно.
/// Идемпотентно: если флаг уже выставлен (повторный вызов, гонка двух вкладок), зорки повторно не списываются —
/// просто возвращается уже актуальный полный список.
/// </summary>
public sealed class RevealIncomingLikesCommandHandler(
    IUserRepository userRepository,
    ILikesRepository likesRepository,
    IPrivacySettingsRepository privacySettingsRepository,
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<RevealIncomingLikesCommand, RevealIncomingLikesResult>
{
    public async Task<RevealIncomingLikesResult> Handle(RevealIncomingLikesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var sparksSpent = 0;

        if (!user.LikesRevealed)
        {
            sparksSpent = sparksOptions.Value.LikesRevealCost;
            await sparksService.SpendAsync(user, sparksSpent, SparkTransactionType.LikesReveal, referenceId: null, cancellationToken);
            user.LikesRevealed = true;

            try
            {
                await userRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrentUserUpdateException ex)
            {
                throw new LikesRevealConflictException(request.UserId, ex);
            }
        }

        var entries = await likesRepository.GetIncomingAsync(request.UserId, cancellationToken);
        var privacyByUserId = await privacySettingsRepository.GetByUserIdsAsync(
            entries.Select(e => e.User.Id).ToHashSet(), cancellationToken);
        return new RevealIncomingLikesResult(sparksSpent, user.SparksBalance, LikeResultMapper.ToUserResults(entries, privacyByUserId));
    }
}
