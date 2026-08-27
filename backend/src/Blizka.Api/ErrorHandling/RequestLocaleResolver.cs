using Microsoft.AspNetCore.Http;

namespace Blizka.Api.ErrorHandling;

/// <summary>
/// Резолвит локаль ответа: сначала явный query-параметр `?locale=`, затем `Accept-Language`, затем JWT-claim
/// `locale` (язык Telegram-профиля на момент входа) и только потом дефолт. Язык интерфейса мини-аппа пользователь
/// выбирает сам и он может отличаться от языка Telegram — раньше claim стоял первым, и сервер отвечал на языке
/// Telegram-профиля, даже когда клиент явно просил другой (баг из тикета ClickUp, найден при интеграции фронта).
/// </summary>
public static class RequestLocaleResolver
{
    public static ApiLocale Resolve(HttpContext httpContext)
    {
        var query = httpContext.Request.Query["locale"].ToString();
        if (ApiLocaleParser.TryParse(query, out var fromQuery))
        {
            return fromQuery;
        }

        var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
        var preferred = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(';')[0])
            .FirstOrDefault();

        if (ApiLocaleParser.TryParse(preferred, out var fromHeader))
        {
            return fromHeader;
        }

        var claim = httpContext.User.FindFirst("locale")?.Value;
        return ApiLocaleParser.TryParse(claim, out var fromClaim) ? fromClaim : ApiLocaleParser.Default;
    }
}
