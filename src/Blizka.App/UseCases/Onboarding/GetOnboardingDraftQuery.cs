using MediatR;

namespace Blizka.App.UseCases.Onboarding;

public sealed record GetOnboardingDraftQuery(Guid UserId) : IRequest<OnboardingDraftResult>;
