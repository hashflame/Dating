using System.Security.Cryptography;
using System.Text;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.DevSeed;
using Blizka.App.Telegram;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Blizka.Api.Auth;

/// <summary>
/// Проверяет сырой заголовок Telegram WebApp <c>X-Telegram-InitData</c> на эндпоинте обмена
/// (T-1.1) и кладёт распарсенный, верифицированный payload в <see cref="HttpContext.Items"/> для
/// <c>AuthController</c>. Все остальные маршруты проходят без изменений — после того как клиент
/// обменял initData на JWT, последующие запросы аутентифицируются через стандартный JWT bearer handler.
/// <para>
/// Спека 003 (docs/specs/003-demo-seed-data.md): здесь же — dev-логин в обход Telegram, для тестирования
/// фронтенда в обычном браузере без initData. Включается только если на сервере явно задан
/// <c>DevLogin:Secret</c> (пусто по умолчанию) и клиент прислал заголовки <see cref="DevLoginSecretHeaderName"/>
/// + <see cref="DevLoginTelegramIdHeaderName"/> с верным секретом. TelegramId необязательно принадлежит одному
/// из 10 демо-пользователей (<see cref="DemoSeedCatalog"/>): если это один из них — логинимся с его именем/username
/// из каталога, иначе (сознательное расширение спеки 003, п. Out) — с синтетическим именем "Dev {id}", и дальше
/// как обычный Telegram-логин: существующий пользователь с таким TelegramId — вход под ним, отсутствующий —
/// создание нового и, значит, онбординг. Секрет общий с остальным окружением (нет отдельного staging), поэтому
/// это осознанно принятый риск: тот, кто знает секрет и TelegramId существующего пользователя (в т.ч. реального),
/// может зайти под ним.
/// </para>
/// </summary>
public sealed class TelegramAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TelegramAuthMiddleware> logger)
{
    public const string HeaderName = "X-Telegram-InitData";
    public const string ItemsKey = "TelegramInitData";
    public const string DevLoginSecretHeaderName = "X-Dev-Login-Secret";
    public const string DevLoginTelegramIdHeaderName = "X-Dev-Login-TelegramId";

    private const string TargetPath = "/api/auth/telegram";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments(TargetPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var configuredSecret = configuration["DevLogin:Secret"];
        var devSecretHeader = context.Request.Headers[DevLoginSecretHeaderName].ToString();
        if (!string.IsNullOrEmpty(configuredSecret) && !string.IsNullOrEmpty(devSecretHeader))
        {
            await HandleDevLoginAsync(context, configuredSecret, devSecretHeader);
            return;
        }

        var initData = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(initData))
        {
            await RejectAsync(context, "missing X-Telegram-InitData header");
            return;
        }

        var botToken = configuration["Telegram:BotToken"] ?? string.Empty;

        if (!TelegramInitDataValidator.TryValidate(initData, botToken, DateTimeOffset.UtcNow, out var parsed, out var failureReason))
        {
            await RejectAsync(context, failureReason ?? "invalid initData");
            return;
        }

        context.Items[ItemsKey] = parsed;

        await next(context);
    }

    private async Task HandleDevLoginAsync(HttpContext context, string configuredSecret, string providedSecret)
    {
        if (!FixedTimeEquals(configuredSecret, providedSecret))
        {
            await RejectDevAsync(context, "wrong X-Dev-Login-Secret");
            return;
        }

        var telegramIdHeader = context.Request.Headers[DevLoginTelegramIdHeaderName].ToString();
        if (!long.TryParse(telegramIdHeader, out var telegramId))
        {
            await RejectDevAsync(context, "missing or malformed X-Dev-Login-TelegramId header");
            return;
        }

        var demoUser = DemoSeedCatalog.FindByTelegramId(telegramId);

        context.Items[ItemsKey] = demoUser is not null
            ? new TelegramInitData(
                demoUser.TelegramId,
                demoUser.FirstName,
                demoUser.LastName,
                demoUser.Username,
                PhotoUrl: null,
                LanguageCode: "ru",
                DateTimeOffset.UtcNow)
            : new TelegramInitData(
                telegramId,
                $"Dev {telegramId}",
                LastName: null,
                Username: null,
                PhotoUrl: null,
                LanguageCode: "ru",
                DateTimeOffset.UtcNow);

        await next(context);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private async Task RejectDevAsync(HttpContext context, string internalReason)
    {
        logger.LogWarning("Rejected dev-login for {Path}: {Reason}", context.Request.Path, internalReason);

        var locale = RequestLocaleResolver.Resolve(context);
        var message = ErrorMessageCatalog.Resolve(ErrorMessageCatalog.DevAccessDenied, locale);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            ApiErrorResponse.From(ErrorMessageCatalog.DevAccessDenied, message));
    }

    private async Task RejectAsync(HttpContext context, string internalReason)
    {
        logger.LogWarning("Rejected Telegram initData for {Path}: {Reason}", context.Request.Path, internalReason);

        var locale = RequestLocaleResolver.Resolve(context);
        var message = ErrorMessageCatalog.Resolve(ErrorMessageCatalog.TelegramInitDataInvalid, locale);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            ApiErrorResponse.From(ErrorMessageCatalog.TelegramInitDataInvalid, message));
    }
}
