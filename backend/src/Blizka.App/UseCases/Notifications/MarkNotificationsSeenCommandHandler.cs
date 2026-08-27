using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Notifications;

public sealed class MarkNotificationsSeenCommandHandler(IUserRepository userRepository, IValidator<MarkNotificationsSeenCommand> validator)
    : IRequestHandler<MarkNotificationsSeenCommand>
{
    public async Task Handle(MarkNotificationsSeenCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var now = DateTimeOffset.UtcNow;

        if (request.Likes)
        {
            user.LastSeenLikesAt = now;
        }

        if (request.Matches)
        {
            user.LastSeenMatchesAt = now;
        }

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            throw new NotificationsSeenConflictException(request.UserId, ex);
        }
    }
}
