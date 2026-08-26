using MediatR;

namespace Blizka.App.UseCases.Blocks;

/// <summary>Снимает блокировку с другого пользователя (T-16.2) — идемпотентно.</summary>
public sealed record UnblockUserCommand(Guid BlockerUserId, Guid BlockedUserId) : IRequest;
