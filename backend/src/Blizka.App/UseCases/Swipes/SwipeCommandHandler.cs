using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Notifications;
using Blizka.App.Sparks;
using Blizka.App.Subscriptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Swipes;

/// <summary>
/// Обрабатывает лайк/дизлайк/суперлайк (T-5.2): проверяет, что цель существует и ещё не свайпнута, при
/// суперлайке списывает зорки, создаёт <see cref="Swipe"/> и, если лайк оказался взаимным, — <see cref="Match"/>.
/// Всё — одним <c>SaveChangesAsync</c> (<see cref="ISwipeRepository"/>), то есть одной DB-транзакцией.
/// </summary>
public sealed class SwipeCommandHandler(
    IUserRepository userRepository,
    ISwipeRepository swipeRepository,
    IMatchRepository matchRepository,
    IUserBlockRepository userBlockRepository,
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions,
    IValidator<SwipeCommand> validator,
    ISubscriptionChecker? subscriptionChecker = null,
    INotificationService? notificationService = null)
    : IRequestHandler<SwipeCommand, SwipeResult>
{
    public async Task<SwipeResult> Handle(SwipeCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var fromUser = await userRepository.GetByIdAsync(request.FromUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.FromUserId} not found.");

        var toUser = await userRepository.GetByIdAsync(request.ToUserId, cancellationToken)
            ?? throw new SwipeTargetNotFoundException(request.ToUserId);

        // T-16.2 — блокировка (в любом направлении) делает цель недоступной для свайпа, как если бы её
        // профиль не существовал: та же ветка/ошибка, что и для реально отсутствующего пользователя.
        if (await userBlockRepository.ExistsEitherDirectionAsync(request.FromUserId, request.ToUserId, cancellationToken))
        {
            throw new SwipeTargetNotFoundException(request.ToUserId);
        }

        if (await swipeRepository.ExistsActiveAsync(request.FromUserId, request.ToUserId, cancellationToken))
        {
            throw new AlreadySwipedException(request.ToUserId);
        }

        // Дневной лимит свайпов (spec 002, B3) — снимается подпиской «Безлимит» (точка расширения T-8.3,
        // сама проверка подписки не реализуется здесь).
        if (subscriptionChecker is null || !await subscriptionChecker.HasUnlimitedSwipesAsync(request.FromUserId, cancellationToken))
        {
            var since = DateTimeOffset.UtcNow.AddHours(-24);
            var usedToday = await swipeRepository.CountSinceAsync(request.FromUserId, since, cancellationToken);
            if (usedToday >= SwipeLimits.DailyLimit)
            {
                var oldestCreatedAt = await swipeRepository.GetOldestCreatedAtSinceAsync(request.FromUserId, since, cancellationToken);
                throw new DailySwipeLimitExceededException(request.FromUserId, (oldestCreatedAt ?? DateTimeOffset.UtcNow).AddHours(24));
            }
        }

        if (request.Type == SwipeType.Superlike)
        {
            await sparksService.SpendAsync(
                fromUser, sparksOptions.Value.SuperlikeCost, SparkTransactionType.Superlike, referenceId: null, cancellationToken);
        }

        var swipe = new Swipe
        {
            Id = Guid.NewGuid(),
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            Type = request.Type,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await swipeRepository.AddAsync(swipe, cancellationToken);

        MatchResult? matchResult = null;

        // Взаимный лайк создаёт мэтч только если встречный свайп уже закоммичен на момент этой проверки —
        // при двух почти одновременных взаимных лайках возможно (крайне маловероятное) окно, где оба свайпа
        // сохранятся, но ни один не увидит другой и мэтч не создастся. Осознанно не решается сериализуемой
        // транзакцией/ретраем — не задано ни decomposition.md, ни spec.md, а цена ошибки для MVP невелика.
        if (request.Type is SwipeType.Like or SwipeType.Superlike &&
            await swipeRepository.HasActiveMutualLikeAsync(request.FromUserId, request.ToUserId, cancellationToken))
        {
            var (user1Id, user2Id) = request.FromUserId.CompareTo(request.ToUserId) < 0
                ? (request.FromUserId, request.ToUserId)
                : (request.ToUserId, request.FromUserId);

            var match = new Match
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                Status = MatchStatus.Active,
                MatchedAt = DateTimeOffset.UtcNow,
            };
            await matchRepository.AddAsync(match, cancellationToken);

            matchResult = new MatchResult(match.Id, toUser.Id, toUser.Name, IcebreakerCatalog.Default);
        }

        try
        {
            await swipeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentSwipeCreationException ex)
        {
            throw new AlreadySwipedException(request.ToUserId, ex);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            throw new SwipeConflictException(request.FromUserId, ex);
        }

        if (matchResult is not null && notificationService is not null)
        {
            // T-10.2 — уведомляем обоих участников; после успешного SaveChangesAsync, чтобы не слать уведомление
            // о мэтче, которого в итоге не случилось (ConcurrentUserUpdateException выше). CancellationToken.None,
            // а не request-токен: мэтч уже закоммичен, и отмена HTTP-запроса клиентом (например, ушёл с экрана)
            // не должна превращать уже успешную операцию в исключение наружу.
            await notificationService.NotifyMatchAsync(request.FromUserId, toUser.Name, CancellationToken.None);
            await notificationService.NotifyMatchAsync(request.ToUserId, fromUser.Name, CancellationToken.None);
        }

        return new SwipeResult(request.Type, matchResult is not null, matchResult, fromUser.SparksBalance);
    }
}
