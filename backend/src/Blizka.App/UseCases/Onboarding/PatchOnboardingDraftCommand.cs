using System.Text.Json;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

public sealed record PatchOnboardingDraftCommand(Guid UserId, int Step, JsonElement Data)
    : IRequest<OnboardingDraftResult>;

public sealed record OnboardingDraftResult(int Step, JsonElement Data);
