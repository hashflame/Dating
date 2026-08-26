using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Blocks;

public sealed class UnblockUserCommandHandler(IUserBlockRepository userBlockRepository) : IRequestHandler<UnblockUserCommand>
{
    public Task Handle(UnblockUserCommand request, CancellationToken cancellationToken) =>
        userBlockRepository.RemoveAsync(request.BlockerUserId, request.BlockedUserId, cancellationToken);
}
