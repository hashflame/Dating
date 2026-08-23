using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

public sealed record CompleteOnboardingCommand(Guid UserId) : IRequest<CompleteOnboardingResult>;

public sealed record CompleteOnboardingResult(
    int SparksAwarded, int ProfileCompleteness, NextProfileReward? NextReward, UserStatus UserStatus);

/// <summary>Ближайший ещё не достигнутый порог ProfileCompleteness и награда за него — стимул на S-07 заполнить профиль дальше.</summary>
/// <param name="Hint">Локализованная подсказка, что заполнить для этого порога (spec 002, B9).</param>
public sealed record NextProfileReward(int Threshold, int SparksReward, string Hint);
