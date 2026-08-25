using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Soft delete аккаунта (T-16.2): <c>Status = Deleted</c>, <c>DeletedAt = now()</c>. Профиль, фото, интересы и
/// мэтчи не стираются — данные хранятся 30 дней (окно на восстановление/юридическое удержание), физическая
/// очистка — отдельная будущая задача (не описана в decomposition.md). Идемпотентно, по образцу T-7.3
/// (UnlockContactCommandHandler): повторный вызов на уже удалённом аккаунте не бросает ошибку.
/// </summary>
public sealed class DeleteAccountCommandHandler(IUserRepository userRepository) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (user.Status == UserStatus.Deleted)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        user.Status = UserStatus.Deleted;
        user.DeletedAt = now;
        user.UpdatedAt = now;

        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
