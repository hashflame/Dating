using MediatR;

namespace Blizka.App.UseCases.Consent;

/// <summary>
/// <c>GET /api/users/me/consent</c> — статус согласия по каждому <c>ConsentType</c>, чтобы клиент мог узнать,
/// дано ли согласие, не полагаясь на <c>OnboardingDraft.Step</c> (T-2.2).
/// </summary>
public sealed record GetUserConsentStatusQuery(Guid UserId) : IRequest<IReadOnlyList<UserConsentStatusResult>>;
