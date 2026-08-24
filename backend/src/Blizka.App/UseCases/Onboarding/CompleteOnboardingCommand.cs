using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

/// <param name="Locale">
/// Локаль текущего запроса ("ru"/"be"/"en"), которой локализуется <see cref="NextProfileReward.Hint"/> —
/// резолвится в Api-слое (JWT-claim, затем Accept-Language) тем же <c>RequestLocaleResolver</c>, что и
/// остальные сообщения об ошибках API, а не берётся из персистентной <see cref="Domain.Entities.User.Locale"/>,
/// которая фиксируется один раз при регистрации и может разойтись с текущим языком интерфейса клиента.
/// </param>
public sealed record CompleteOnboardingCommand(Guid UserId, string Locale) : IRequest<CompleteOnboardingResult>;

public sealed record CompleteOnboardingResult(
    int SparksAwarded, int ProfileCompleteness, NextProfileReward? NextReward, UserStatus UserStatus);

/// <summary>Ближайший ещё не достигнутый порог ProfileCompleteness и награда за него — стимул на S-07 заполнить профиль дальше.</summary>
/// <param name="Hint">Локализованная подсказка, что заполнить для этого порога (spec 002, B9).</param>
public sealed record NextProfileReward(int Threshold, int SparksReward, string Hint);
