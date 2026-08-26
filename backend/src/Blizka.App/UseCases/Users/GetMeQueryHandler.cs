using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Onboarding;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Отдаёт полный профиль текущего пользователя (T-9.1): грузит пользователя вместе с фото/интересами
/// (нужны только для <see cref="ProfileCompletenessCalculator.Calculate"/>, сам список фото/интересов
/// в ответ не попадает — под них отдельные эндпоинты, T-3.1/T-9.2), пересчитывает ProfileCompleteness
/// "по требованию" (без побочных начислений — пороговые бонусы начисляются только при изменении профиля,
/// см. <see cref="PatchUserProfileCommandHandler"/>/<see cref="Onboarding.CompleteOnboardingCommandHandler"/>,
/// а не на каждом GET).
/// </summary>
public sealed class GetMeQueryHandler(
    IUserRepository userRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<GetMeQuery, GetMeResult>
{
    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var datePreferenceCount = await datePreferenceRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        var completeness = ProfileCompletenessCalculator.Calculate(user, datePreferenceCount);
        var nextReward = ProfileCompletenessCalculator.NextReward(
            completeness, request.Locale, sparksOptions.Value.ProfileCompletionThresholdBonusAmount);

        return UserProfileMapper.ToResult(user, completeness, nextReward, request.Locale);
    }
}
