using Microsoft.AspNetCore.Http;

namespace Blizka.Api.ErrorHandling;

/// <summary>Резолвит локаль для ответа об ошибке: сначала JWT-claim `locale`, затем Accept-Language, затем дефолт.</summary>
public static class RequestLocaleResolver
{
    public static ApiLocale Resolve(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst("locale")?.Value;
        if (ApiLocaleParser.TryParse(claim, out var fromClaim))
        {
            return fromClaim;
        }

        var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
        var preferred = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(';')[0])
            .FirstOrDefault();

        return ApiLocaleParser.TryParse(preferred, out var fromHeader) ? fromHeader : ApiLocaleParser.Default;
    }
}
