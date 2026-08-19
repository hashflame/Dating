using MediatR;

namespace Blizka.App.UseCases.Onboarding;

public sealed record CompleteOnboardingCommand(Guid UserId) : IRequest<CompleteOnboardingResult>;

public sealed record CompleteOnboardingResult(int SparksAwarded, int ProfileCompleteness, NextProfileReward? NextReward);

/// <summary>Ближайший ещё не достигнутый порог ProfileCompleteness и награда за него — стимул на S-07 заполнить профиль дальше.</summary>
public sealed record NextProfileReward(int Threshold, int SparksReward);
