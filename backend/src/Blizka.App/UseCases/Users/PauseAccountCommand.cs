using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Ставит аккаунт текущего пользователя на паузу (T-16.2) — скрывает из ленты, мэтчи сохраняются.</summary>
public sealed record PauseAccountCommand(Guid UserId) : IRequest;
