using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Общая часть "вернуть пользователя к состоянию сразу после онбординга" — используется
/// <see cref="ResetUserStateCommandHandler"/> (dev-инструмент, T-16.2) и восстановлением аккаунта после
/// повторного входа с <c>Status = Deleted</c> (<c>AuthenticateTelegramUserCommandHandler</c>, тикет ClickUp:
/// раньше такой вход навсегда отдавал 410). Чистит лайки/дизлайки (обе стороны), мэтчи, фото, интересы,
/// предпочтения на свидания и необязательные поля профиля. Не трогает <c>RegistrationBonusAwardedAt</c>,
/// пороговые бонусы заполненности, реферальные связи, жалобы и историю блокировок — вызывающий код решает
/// сам, что делать с этими полями (у dev-сброса и у восстановления аккаунта разные правила: сброс обнуляет
/// пороговые бонусы для повторного тестирования, восстановление — намеренно нет, иначе фарм зорок циклом
/// "удалил → вошёл → снова заработал").
/// </summary>
internal static class UserStateResetter
{
    public static async Task ClearActivityAndOptionalProfileAsync(
        User user,
        ISwipeRepository swipeRepository,
        IMatchRepository matchRepository,
        IPhotoRepository photoRepository,
        CancellationToken cancellationToken)
    {
        await swipeRepository.RemoveAllInvolvingUserAsync(user.Id, cancellationToken);
        await matchRepository.RemoveAllForUserAsync(user.Id, cancellationToken);

        foreach (var photo in await photoRepository.GetByUserIdAsync(user.Id, cancellationToken))
        {
            photoRepository.Remove(photo);
        }

        user.UserInterests.Clear();
        user.UserDatePreferences.Clear();

        user.Bio = null;
        user.Height = null;
        user.Smoking = null;
        user.Drinking = null;
        user.Chronotype = null;
        user.Prompts = [];
        user.InstagramHandle = null;
        user.VoiceIntroUrl = null;
        user.IsVerified = false;
        user.LikesRevealed = false;
    }
}
