using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// Обрабатывает <see cref="GetFeedFiltersQuery"/> (T-5.4): возвращает сохранённый <c>UserFilter</c>, либо,
/// если пользователь его ещё не создавал (бэкафилла для уже онбордившихся нет), MVP-дефолты — по тому же
/// принципу "пустое состояние", что и <c>GetOnboardingDraftQueryHandler</c> для черновика.
/// </summary>
public sealed class GetFeedFiltersQueryHandler(IUserFilterRepository filterRepository, IUserRepository userRepository)
    : IRequestHandler<GetFeedFiltersQuery, FeedFiltersResult>
{
    public async Task<FeedFiltersResult> Handle(GetFeedFiltersQuery request, CancellationToken cancellationToken)
    {
        var filter = await filterRepository.GetAsync(request.UserId, cancellationToken);
        if (filter is not null)
        {
            return FeedFiltersResult.From(filter);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        return FeedFiltersResult.Default(UserFilterDefaults.ResolveDefaultShowGender(user.Gender));
    }
}
