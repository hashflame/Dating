using Blizka.App.Domain.Services;

namespace Blizka.Api.Dev;

/// <summary>Ответ <c>POST /api/dev/reseed-demo-data</c> (спека 003) — список демо-аккаунтов для dev-логина.</summary>
public sealed record ReseedDemoDataResponse(IReadOnlyList<DemoSeedUserDto> Users)
{
    public static ReseedDemoDataResponse From(IReadOnlyList<DemoSeedResultUser> users) =>
        new(users.Select(DemoSeedUserDto.From).ToArray());
}

/// <param name="TelegramId">Значение для заголовка <c>X-Dev-Login-TelegramId</c>.</param>
/// <param name="Username">Telegram-username демо-пользователя.</param>
/// <param name="Name">Имя демо-пользователя.</param>
/// <param name="MainPhotoUrl">URL главного фото или <c>null</c>, если фото не загрузились.</param>
public sealed record DemoSeedUserDto(long TelegramId, string Username, string Name, string? MainPhotoUrl)
{
    public static DemoSeedUserDto From(DemoSeedResultUser result) =>
        new(result.TelegramId, result.Username, result.Name, result.MainPhotoUrl);
}
