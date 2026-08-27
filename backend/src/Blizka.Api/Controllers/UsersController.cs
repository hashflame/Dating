using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Consent;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Photos;
using Blizka.Api.Users;
using Blizka.App.UseCases.Consent;
using Blizka.App.UseCases.DatePreferences;
using Blizka.App.UseCases.Interests;
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
    /// Полный профиль текущего пользователя (T-9.1): id, telegramId, редактируемые поля профиля, баланс
    /// зорок, статус аккаунта, заполненность и ближайшая награда за неё.
    /// </summary>
    /// <response code="200">Профиль найден.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<UserMeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMeQuery(User.GetUserId(), ResolveLocale()), cancellationToken);

        return Ok(ApiResponse<UserMeResponse>.Ok(UserMeResponse.From(result)));
    }

    /// <summary>
    /// Частично обновляет профиль текущего пользователя (T-9.1): name, bio, height, smoking, drinking,
    /// chronotype, prompts, datingGoal. Не переданное (<c>null</c>) поле не меняется. Пересчитывает
    /// ProfileCompleteness и начисляет бонус за впервые достигнутый порог (60/80/100%).
    /// </summary>
    /// <response code="200">Профиль обновлён.</response>
    /// <response code="400">Тело запроса не прошло валидацию.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Параллельный PATCH того же пользователя уже сохранился первым — повторите запрос.</response>
    [HttpPatch("profile")]
    [ProducesResponseType<ApiResponse<PatchUserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchProfile(PatchUserProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new PatchUserProfileCommand(
            User.GetUserId(),
            request.Name,
            request.Bio,
            request.Height,
            request.Smoking,
            request.Drinking,
            request.Chronotype,
            request.Prompts,
            request.DatingGoal,
            ResolveLocale());

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PatchUserProfileResponse>.Ok(PatchUserProfileResponse.From(result)));
    }

    /// <summary>
    /// Задаёт полный набор интересов текущего пользователя (T-9.2): <c>interestIds</c> — уже существующие в
    /// каталоге, <c>customInterests</c> — названия новых кастомных (создаются и становятся общими для всех).
    /// Заменяет весь набор целиком, как и <c>prompts</c> в <c>PATCH /users/me/profile</c>. Пересчитывает
    /// ProfileCompleteness и начисляет бонус за впервые достигнутый порог (60/80/100%).
    /// </summary>
    /// <response code="200">Интересы обновлены.</response>
    /// <response code="400">Тело запроса не прошло валидацию (пустое название кастомного интереса, больше 20 интересов суммарно и т.п.).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Один из переданных <c>interestIds</c> отсутствует в каталоге.</response>
    /// <response code="409">Параллельный PATCH того же пользователя уже сохранился первым — повторите запрос.</response>
    [HttpPatch("interests")]
    [ProducesResponseType<ApiResponse<PatchUserInterestsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchInterests(PatchUserInterestsRequest request, CancellationToken cancellationToken)
    {
        var command = new PatchUserInterestsCommand(
            User.GetUserId(),
            request.InterestIds ?? [],
            request.CustomInterests ?? [],
            ResolveLocale());

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PatchUserInterestsResponse>.Ok(PatchUserInterestsResponse.From(result)));
    }

    /// <summary>
    /// Задаёт полный набор предпочтений по формату свидания текущего пользователя (T-9.3): каталог
    /// фиксированный (4 значения), заменяет весь набор целиком — как и <c>interestIds</c> в
    /// <c>PATCH /users/me/interests</c>. Пересчитывает ProfileCompleteness и начисляет бонус за впервые
    /// достигнутый порог (60/80/100%). Учитывается в скоринге ленты (T-5.1) как дополнительный фактор совместимости.
    /// </summary>
    /// <response code="200">Предпочтения обновлены.</response>
    /// <response code="400">Тело запроса не прошло валидацию.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Параллельный PATCH того же пользователя уже сохранился первым — повторите запрос.</response>
    [HttpPatch("date-preferences")]
    [ProducesResponseType<ApiResponse<PatchUserDatePreferencesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchDatePreferences(PatchUserDatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var command = new PatchUserDatePreferencesCommand(User.GetUserId(), request.Preferences ?? [], ResolveLocale());

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PatchUserDatePreferencesResponse>.Ok(PatchUserDatePreferencesResponse.From(result)));
    }

    /// <summary>Профиль текущего пользователя в формате карточки ленты — как его видят другие (T-9.1).</summary>
    /// <response code="200">Карточка-превью профиля.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("preview")]
    [ProducesResponseType<ApiResponse<ProfilePreviewResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfilePreview(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProfilePreviewQuery(User.GetUserId(), ResolveLocale()), cancellationToken);

        return Ok(ApiResponse<ProfilePreviewResponse>.Ok(ProfilePreviewResponse.From(result)));
    }

    // Та же локаль запроса ("ru"/"be"/"en"), которой резолвятся сообщения об ошибках (RequestLocaleResolver) и
    // NextReward.Hint при завершении онбординга (OnboardingController.Complete) — не персистентная User.Locale.
    private string ResolveLocale() => RequestLocaleResolver.Resolve(HttpContext) switch
    {
        ApiLocale.Be => "be",
        ApiLocale.En => "en",
        _ => "ru",
    };

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
    /// <response code="409">Это последнее фото пользователя — сначала нужно загрузить новое.</response>
    [HttpDelete("photos/{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Ставит аккаунт текущего пользователя на паузу (T-16.2): <c>Status = Paused</c>, профиль скрыт из ленты,
    /// существующие мэтчи сохраняются. Идемпотентно — повторный вызов на уже стоящем на паузе аккаунте тоже
    /// возвращает 204.
    /// </summary>
    /// <response code="204">Аккаунт поставлен на паузу.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Pause(CancellationToken cancellationToken)
    {
        await mediator.Send(new PauseAccountCommand(User.GetUserId()), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Снимает аккаунт текущего пользователя с паузы (T-16.2): <c>Status = Active</c>, профиль снова виден в
    /// ленте. Не действует на аккаунт в любом другом статусе (в том числе удалённый/забаненный) — тихо
    /// ничего не делает, чтобы не воскресить их через resume.
    /// </summary>
    /// <response code="204">Аккаунт снят с паузы.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Resume(CancellationToken cancellationToken)
    {
        await mediator.Send(new ResumeAccountCommand(User.GetUserId()), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Запускает фоновую сборку JSON-архива данных текущего пользователя (T-16.2): профиль, фото, интересы,
    /// согласия, настройки приватности. Сама выгрузка асинхронная — эндпоинт только ставит запрос в очередь
    /// и сразу отвечает, ссылка на готовый архив придёт отдельным сообщением в Telegram.
    /// </summary>
    /// <response code="202">Запрос принят, архив собирается в фоне.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("data-export")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestDataExport(CancellationToken cancellationToken)
    {
        await mediator.Send(new RequestDataExportCommand(User.GetUserId()), cancellationToken);

        return Accepted();
    }

    private static PhotoResponse ToResponse(PhotoResult result) =>
        new(result.Id, result.Url, result.ThumbnailUrl, result.MediumUrl, result.SortOrder, result.IsMain, result.CreatedAt);
}
