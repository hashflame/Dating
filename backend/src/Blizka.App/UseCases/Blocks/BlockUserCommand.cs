using MediatR;

namespace Blizka.App.UseCases.Blocks;

/// <summary>Блокирует другого пользователя (T-16.2) — идемпотентно, повторный вызов ничего не меняет.</summary>
public sealed record BlockUserCommand(Guid BlockerUserId, Guid BlockedUserId) : IRequest;
