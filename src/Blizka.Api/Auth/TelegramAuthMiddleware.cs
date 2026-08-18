using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
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
/// </summary>
public sealed class TelegramAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TelegramAuthMiddleware> logger)
{
    public const string HeaderName = "X-Telegram-InitData";
    public const string ItemsKey = "TelegramInitData";

    private const string TargetPath = "/api/auth/telegram";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments(TargetPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
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
