using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Снимает аккаунт текущего пользователя с паузы (T-16.2) — снова виден в ленте.</summary>
public sealed record ResumeAccountCommand(Guid UserId) : IRequest;
