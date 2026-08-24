using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Users;

public sealed class GetMeQueryHandler(IUserRepository userRepository) : IRequestHandler<GetMeQuery, GetMeResult>
{
    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        return new GetMeResult(user.Id, user.TelegramId, user.Name, user.SparksBalance, user.Status, user.Locale);
    }
}
