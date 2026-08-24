using MediatR;

namespace Blizka.App.UseCases.Onboarding;

public sealed record DeleteOnboardingDraftCommand(Guid UserId) : IRequest;
