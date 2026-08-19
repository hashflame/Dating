namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>Photo</c> конфликтует с одним из уникальных
/// индексов, защищающих инварианты фото (не более одного <c>IsMain</c> и не более одного фото на
/// <c>(UserId, SortOrder)</c>) — т.е. два параллельных <c>POST /api/users/me/photos</c> для одного и того же
/// пользователя (например, двойной тап на медленной сети) одновременно прочитали одно и то же количество
/// фото и попытались занять один и тот же <c>SortOrder</c>/<c>IsMain</c>.
/// Предназначено для внутреннего перезапроса в вызывающем коде (с пересчитанным SortOrder/IsMain), а не для показа клиенту.
/// </summary>
public sealed class ConcurrentPhotoUploadException(Guid userId, Exception innerException)
    : Exception($"Photo for user {userId} was uploaded concurrently.", innerException)
{
    public Guid UserId { get; } = userId;
}
