namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается при попытке удалить последнее оставшееся фото пользователя (T-3.1) — онбординг требует
/// минимум 1 фото для завершения регистрации (см. <c>CompleteOnboardingCommandHandler</c>), а без этой
/// проверки активный пользователь мог удалить их все по одному и получить анкету без единого фото
/// (баг из e2e-прогона).
/// </summary>
public sealed class LastPhotoDeletionException(Guid userId)
    : BlizkaDomainException(
        "LAST_PHOTO_DELETION_FORBIDDEN",
        $"User {userId} cannot delete their last remaining photo.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
