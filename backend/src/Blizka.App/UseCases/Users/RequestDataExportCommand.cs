using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Ставит в очередь фоновую сборку JSON-архива данных текущего пользователя (T-16.2). Ссылка на архив придёт в Telegram.</summary>
public sealed record RequestDataExportCommand(Guid UserId) : IRequest;
