using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Снимает паузу, только если аккаунт реально стоит на паузе — на остальных статусах (в том числе
/// <c>Deleted</c>/<c>Banned</c>/<c>Shadowbanned</c>) ничего не делает, чтобы не воскресить их через resume.
/// </summary>
public sealed class ResumeAccountCommandHandler(IUserRepository userRepository) : IRequestHandler<ResumeAccountCommand>
{
    public async Task Handle(ResumeAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (user.Status != UserStatus.Paused)
        {
            return;
        }

        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
