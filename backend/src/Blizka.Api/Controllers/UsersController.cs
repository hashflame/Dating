using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Consent;
using Blizka.Api.Photos;
using Blizka.Api.Users;
using Blizka.App.UseCases.Consent;
using Blizka.App.UseCases.Photos;
using Blizka.App.UseCases.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Действия текущего пользователя над собственным профилем.</summary>
[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Минимальный профиль текущего пользователя: id, telegramId, имя, баланс зорок, статус аккаунта
    /// (по нему клиент понимает, завершён ли онбординг) — нужен, например, чтобы показать баланс зорок
    /// на главном экране. Полный профиль (bio, completeness, nextReward) — отдельная будущая задача (T-9.1).
    /// </summary>
    /// <response code="200">Профиль найден.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<UserMeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMeQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<UserMeResponse>.Ok(
            new UserMeResponse(result.Id, result.TelegramId, result.Name, result.SparksBalance, result.Status, result.Locale)));
    }

    /// <summary>
    /// Фиксирует юридическое согласие пользователя (T-2.2) с временной меткой, IP-адресом и Telegram id —
    /// на фронте кнопка недоступна без чекбокса, но бэкенд тоже проверяет это при завершении онбординга (defense in depth).
    /// </summary>
    /// <response code="200">Согласие зафиксировано.</response>
    /// <response code="400">Тело запроса не прошло валидацию.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("consent")]
    [ProducesResponseType<ApiResponse<UserConsentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordConsent(RecordConsentRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordUserConsentCommand(
            User.GetUserId(),
            User.GetTelegramId(),
            request.Type,
            request.Version,
            request.AgeConfirmed,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<UserConsentResponse>.Ok(new UserConsentResponse(result.Type, result.Version, result.Timestamp)));
    }

    /// <summary>
    /// Статус согласий пользователя по каждому типу (T-2.2) — чтобы клиент мог узнать, дано ли согласие,
    /// не полагаясь на <c>OnboardingDraft.Step</c>.
    /// </summary>
    /// <response code="200">Статус по каждому типу согласия. Если согласие ещё не дано, это не 404 — тип просто приходит с <c>given: false</c>.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("consent")]
    [ProducesResponseType<ApiResponse<UserConsentStatusResponse[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConsentStatus(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserConsentStatusQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<UserConsentStatusResponse[]>.Ok(result.Select(UserConsentStatusResponse.From).ToArray()));
    }

    /// <summary>Список фото профиля (T-3.1), в порядке <c>SortOrder</c> — чтобы клиент видел их после перезагрузки.</summary>
    /// <response code="200">Список фото (может быть пустым).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("photos")]
    [ProducesResponseType<ApiResponse<PhotoResponse[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPhotos(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPhotosQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<PhotoResponse[]>.Ok(result.Select(ToResponse).ToArray()));
    }

    /// <summary>
    /// Загружает фото профиля (T-3.1): удаляет EXIF, генерирует thumbnail (150px) и medium (600px), заливает
    /// все три варианта в S3-совместимое хранилище (MinIO — локально, см. docker-compose.yml).
    /// </summary>
    /// <response code="200">Фото загружено.</response>
    /// <response code="400">Неподдерживаемый формат/повреждённый файл или размер больше 10MB.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Конфликт с другой одновременной загрузкой того же пользователя — повторите запрос.</response>
    /// <response code="422">У пользователя уже загружено максимум (6) фото.</response>
    [HttpPost("photos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType<ApiResponse<PhotoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadPhotoCommand(User.GetUserId(), stream, file.ContentType, file.Length);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PhotoResponse>.Ok(ToResponse(result)));
    }

    /// <summary>Удаляет фото профиля (T-3.1). Если удалённое фото было главным, главным становится следующее по порядку.</summary>
    /// <response code="204">Фото удалено.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Фото не найдено (в том числе если оно принадлежит другому пользователю).</response>
    [HttpDelete("photos/{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeletePhotoCommand(User.GetUserId(), photoId), cancellationToken);

        return NoContent();
    }

    /// <summary>Переупорядочивает фото профиля и назначает главное (T-3.1).</summary>
    /// <response code="200">Новый порядок сохранён.</response>
    /// <response code="400"><c>order</c> не совпадает с текущим набором фото пользователя, либо <c>mainPhotoId</c> не входит в <c>order</c>.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPatch("photos/reorder")]
    [ProducesResponseType<ApiResponse<PhotoResponse[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderPhotos(ReorderPhotosRequest request, CancellationToken cancellationToken)
    {
        var command = new ReorderPhotosCommand(User.GetUserId(), request.Order, request.MainPhotoId);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PhotoResponse[]>.Ok(result.Select(ToResponse).ToArray()));
    }

    /// <summary>
    /// Импортирует аватар пользователя из Telegram (T-3.1). <c>photoUrl</c> — значение
    /// <c>Telegram.WebApp.initDataUnsafe.user.photo_url</c> на клиенте (сервер его не хранит, см. T-1.1).
    /// Проходит тот же конвейер обработки и лимиты, что и обычная загрузка.
    /// </summary>
    /// <response code="200">Аватар импортирован как новое фото.</response>
    /// <response code="400"><c>photoUrl</c> не является ссылкой на Telegram CDN, либо файл повреждён/не изображение.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="422">У пользователя уже загружено максимум (6) фото.</response>
    [HttpPost("photos/import-telegram")]
    [ProducesResponseType<ApiResponse<PhotoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportTelegramPhoto(ImportTelegramPhotoRequest request, CancellationToken cancellationToken)
    {
        var command = new ImportTelegramPhotoCommand(User.GetUserId(), request.PhotoUrl);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PhotoResponse>.Ok(ToResponse(result)));
    }

    /// <summary>
    /// Удаляет аккаунт текущего пользователя (T-16.2): soft delete — <c>Status = Deleted</c>,
    /// <c>DeletedAt</c> проставляется, но профиль/фото/интересы/мэтчи физически не стираются (окно 30 дней).
    /// Идемпотентно — повторный вызов на уже удалённом аккаунте тоже возвращает 204.
    /// </summary>
    /// <response code="204">Аккаунт помечен удалённым.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAccountCommand(User.GetUserId()), cancellationToken);

        return NoContent();
    }

    private static PhotoResponse ToResponse(PhotoResult result) =>
        new(result.Id, result.Url, result.ThumbnailUrl, result.MediumUrl, result.SortOrder, result.IsMain, result.CreatedAt);
}
