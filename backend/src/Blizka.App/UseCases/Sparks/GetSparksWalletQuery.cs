using MediatR;

namespace Blizka.App.UseCases.Sparks;

/// <summary><c>GET /api/sparks/wallet</c> (T-8.1) — баланс, история операций и способы заработать зорки.</summary>
public sealed record GetSparksWalletQuery(Guid UserId, int Page, int PageSize) : IRequest<SparksWalletResult>;
