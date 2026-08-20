using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
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
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions,
    IValidator<SwipeCommand> validator)
    : IRequestHandler<SwipeCommand, SwipeResult>
{
    public async Task<SwipeResult> Handle(SwipeCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var fromUser = await userRepository.GetByIdAsync(request.FromUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.FromUserId} not found.");

        var toUser = await userRepository.GetByIdAsync(request.ToUserId, cancellationToken)
            ?? throw new SwipeTargetNotFoundException(request.ToUserId);

        if (await swipeRepository.ExistsActiveAsync(request.FromUserId, request.ToUserId, cancellationToken))
        {
            throw new AlreadySwipedException(request.ToUserId);
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

        return new SwipeResult(request.Type, matchResult is not null, matchResult, fromUser.SparksBalance);
    }
}
