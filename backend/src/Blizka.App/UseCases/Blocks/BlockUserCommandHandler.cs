using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Blocks;

/// <summary>
/// Блокирует пользователя (T-16.2). Идемпотентно — если блокировка уже стоит, просто ничего не делает (тот
/// же приём, что и в <see cref="Users.DeleteAccountCommandHandler"/>). Блокировка не удаляет уже случившийся
/// мэтч/переписку — она только скрывает пару друг от друга в дальнейшем (лента, свайпы, T-16.2).
/// </summary>
public sealed class BlockUserCommandHandler(
    IUserRepository userRepository, IUserBlockRepository userBlockRepository, IValidator<BlockUserCommand> validator)
    : IRequestHandler<BlockUserCommand>
{
    public async Task Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var blockedUser = await userRepository.GetByIdAsync(request.BlockedUserId, cancellationToken)
            ?? throw new UserProfileNotFoundException(request.BlockedUserId);

        if (await userBlockRepository.ExistsAsync(request.BlockerUserId, request.BlockedUserId, cancellationToken))
        {
            return;
        }

        await userBlockRepository.AddAsync(
            new UserBlock
            {
                Id = Guid.NewGuid(),
                BlockerUserId = request.BlockerUserId,
                BlockedUserId = blockedUser.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);

        await userBlockRepository.SaveChangesAsync(cancellationToken);
    }
}
