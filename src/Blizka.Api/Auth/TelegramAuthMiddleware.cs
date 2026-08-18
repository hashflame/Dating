using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.Telegram;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Blizka.Api.Auth;

/// <summary>
/// Validates the raw Telegram WebApp <c>X-Telegram-InitData</c> header on the auth exchange endpoint
/// (T-1.1) and stashes the parsed, verified payload on <see cref="HttpContext.Items"/> for
/// <c>AuthController</c> to consume. Every other route passes through untouched — once a client has
/// exchanged initData for a JWT, subsequent requests authenticate via the standard JWT bearer handler.
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
