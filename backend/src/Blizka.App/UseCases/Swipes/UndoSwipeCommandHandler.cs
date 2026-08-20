using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Swipes;

/// <summary>
/// Отменяет последний активный свайп пользователя (T-5.3, не более <see cref="MaxUndosPerDay"/> раз за
/// скользящее окно 24 часа): проставляет <c>Swipe.UndoneAt</c>, при необходимости удаляет мэтч, который этот
/// свайп создал (если контакт по нему ещё не открыт), и возвращает зорки за отменённый суперлайк.
/// Всё — одним <c>SaveChangesAsync</c> (<see cref="ISwipeRepository"/>), то есть одной DB-транзакцией,
/// по тому же паттерну, что и <see cref="SwipeCommandHandler"/> — включая перевод гонки конкурентного
/// сохранения (например, двойное нажатие "отменить" почти одновременно) в <see cref="SwipeConflictException"/>
/// вместо необработанного 500.
/// </summary>
public sealed class UndoSwipeCommandHandler(
    IUserRepository userRepository,
    ISwipeRepository swipeRepository,
    IMatchRepository matchRepository,
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<UndoSwipeCommand, UndoSwipeResult>
{
    private const int MaxUndosPerDay = 3;

    public async Task<UndoSwipeResult> Handle(UndoSwipeCommand request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var undosUsed = await swipeRepository.CountUndoneSinceAsync(request.UserId, since, cancellationToken);
        if (undosUsed >= MaxUndosPerDay)
        {
            throw new UndoLimitExceededException(request.UserId, MaxUndosPerDay);
        }

        var swipe = await swipeRepository.GetLastActiveAsync(request.UserId, cancellationToken)
            ?? throw new NothingToUndoException(request.UserId);

        swipe.UndoneAt = DateTimeOffset.UtcNow;

        if (swipe.Type is SwipeType.Like or SwipeType.Superlike)
        {
            var match = await matchRepository.GetByUsersAsync(swipe.FromUserId, swipe.ToUserId, cancellationToken);
            if (match is not null && match.ContactUnlockedAt is null)
            {
                matchRepository.Remove(match);
            }
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (swipe.Type == SwipeType.Superlike)
        {
            await sparksService.RefundAsync(user, sparksOptions.Value.SuperlikeCost, swipe.Id, cancellationToken);
        }

        try
        {
            await swipeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            throw new SwipeConflictException(request.UserId, ex);
        }

        var undosRemaining = MaxUndosPerDay - (undosUsed + 1);
        return new UndoSwipeResult(swipe.ToUserId, swipe.Type, undosRemaining, user.SparksBalance);
    }
}
