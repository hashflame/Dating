namespace Blizka.Api.Photos;

/// <summary>Фото профиля в ответах API.</summary>
/// <param name="Id">Id фото.</param>
/// <param name="Url">Публичный URL оригинала (без EXIF).</param>
/// <param name="ThumbnailUrl">Публичный URL миниатюры (150px).</param>
/// <param name="MediumUrl">Публичный URL среднего размера (600px).</param>
/// <param name="SortOrder">Позиция в галерее пользователя, начиная с 0.</param>
/// <param name="IsMain">Является ли фото главным (обложкой профиля).</param>
/// <param name="CreatedAt">Момент загрузки.</param>
public sealed record PhotoResponse(
    Guid Id,
    string Url,
    string ThumbnailUrl,
    string MediumUrl,
    int SortOrder,
    bool IsMain,
    DateTimeOffset CreatedAt);

/// <summary>Тело запроса <c>PATCH /api/users/me/photos/reorder</c>.</summary>
/// <param name="Order">Id всех фото пользователя в новом порядке — должен содержать ровно текущий набор без повторов.</param>
/// <param name="MainPhotoId">Id фото, которое становится главным; должен входить в <paramref name="Order"/>.</param>
public sealed record ReorderPhotosRequest(Guid[] Order, Guid MainPhotoId);

/// <summary>Тело запроса <c>POST /api/users/me/photos/import-telegram</c>.</summary>
/// <param name="PhotoUrl">
/// Значение <c>Telegram.WebApp.initDataUnsafe.user.photo_url</c> на клиенте — сервер его не хранит,
/// поэтому клиент присылает его заново на момент импорта. Должно быть ссылкой на Telegram CDN (https://t.me/...).
/// </param>
public sealed record ImportTelegramPhotoRequest(string PhotoUrl);
