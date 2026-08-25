using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Удаление аккаунта текущего пользователя (T-16.2) — soft delete, без физического стирания данных.</summary>
public sealed record DeleteAccountCommand(Guid UserId) : IRequest;
