using MediatR;

namespace Blizka.App.UseCases.Sparks;

/// <summary><c>GET /api/sparks/wallet</c> (T-8.1) — баланс, история операций и способы заработать зорки.</summary>
/// <param name="Locale">Локаль запроса ("ru"/"be"/"en") для <see cref="SparkEarnOptionResult.Label"/> — тот же принцип, что и <c>GetMeQuery.Locale</c>, не персистентная <c>User.Locale</c>.</param>
public sealed record GetSparksWalletQuery(Guid UserId, int Page, int PageSize, string Locale) : IRequest<SparksWalletResult>;
