using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Users;
using Blizka.App.UseCases.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Просмотр чужой анкеты по id — открыть профиль из списка (лайки, T-6.1, и аналогичные урезанные списки).</summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserProfilesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Полная анкета произвольного пользователя — тот же набор полей, что и <c>GET /api/users/me/preview</c>
    /// (T-9.1), без полей, доступных только себе. Нужен, чтобы тапнуть на человека в списке лайков (<c>LikeUserDto</c>
    /// отдаёт только userId/name/age/mainPhotoUrl) и увидеть, кто это, даже если его уже нет в ленте.
    /// </summary>
    /// <response code="200">Анкета найдена.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Анкета не найдена — не существует либо аккаунт удалён.</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<ApiResponse<UserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserProfileQuery(userId, User.GetUserId(), ResolveLocale()), cancellationToken);

        return Ok(ApiResponse<UserProfileResponse>.Ok(UserProfileResponse.From(result)));
    }

    // Та же локаль запроса, что и в UsersController.ResolveLocale (T-9.1) — не персистентная User.Locale.
    private string ResolveLocale() => RequestLocaleResolver.Resolve(HttpContext) switch
    {
        ApiLocale.Be => "be",
        ApiLocale.En => "en",
        _ => "ru",
    };
}
