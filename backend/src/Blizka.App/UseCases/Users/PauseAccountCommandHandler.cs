using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Идемпотентно по образцу <see cref="DeleteAccountCommandHandler"/> — повторный вызов на уже стоящем на паузе аккаунте ничего не меняет.</summary>
public sealed class PauseAccountCommandHandler(IUserRepository userRepository) : IRequestHandler<PauseAccountCommand>
{
    public async Task Handle(PauseAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (user.Status == UserStatus.Paused)
        {
            return;
        }

        user.Status = UserStatus.Paused;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
