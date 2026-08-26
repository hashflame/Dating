using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Subscriptions;
using MediatR;

namespace Blizka.App.UseCases.Privacy;

/// <summary>
/// Обрабатывает <see cref="PatchPrivacySettingsCommand"/> (T-16.1) — частично обновляет настройки приватности,
/// создавая строку лениво при первом вызове (конфликт двух параллельных первых PATCH одного пользователя
/// подхватывается через <see cref="ConcurrentPrivacySettingsCreationException"/>, по образцу
/// <c>PatchFeedFiltersCommandHandler</c>, а не падает в 500). <c>invisibleMode</c> — единственное поле с
/// бизнес-проверкой: включить его может только подписчик «Безлимит» (T-8.3); <see cref="ISubscriptionChecker"/> —
/// та же точка расширения без DI-регистрации, что и в <c>UnlockContactCommandHandler</c>/<c>SwipeCommandHandler</c> —
/// пока T-8.3 не сделана, <paramref name="subscriptionChecker"/> резолвится в <c>null</c>, и включение навсегда отклоняется.
/// </summary>
public sealed class PatchPrivacySettingsCommandHandler(
    IPrivacySettingsRepository repository,
    ISubscriptionChecker? subscriptionChecker = null)
    : IRequestHandler<PatchPrivacySettingsCommand, PrivacySettingsResult>
{
    public async Task<PrivacySettingsResult> Handle(PatchPrivacySettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetByUserIdTrackedAsync(request.UserId, cancellationToken);
        var isNew = settings is null;
        // ShowLastActive не проставляется явно — дефолт true уже задан в PrivacySettings, совпадает с
        // PrivacySettingsDefaults.Result (единственное поле, дефолт которого не false).
        settings ??= new PrivacySettings { Id = Guid.NewGuid(), UserId = request.UserId };

        if (request.InvisibleMode is true && settings.InvisibleMode is false)
        {
            var hasActiveSubscription = subscriptionChecker is not null
                && await subscriptionChecker.HasActiveSubscriptionAsync(request.UserId, cancellationToken);
            if (!hasActiveSubscription)
            {
                throw new InvisibleModeRequiresSubscriptionException(request.UserId);
            }
        }

        ApplyPatch(settings, request);
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        if (isNew)
        {
            await repository.AddAsync(settings, cancellationToken);
        }

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentPrivacySettingsCreationException) when (isNew)
        {
            // Параллельный PATCH того же пользователя успел создать строку первым — подхватываем уже
            // созданную запись и накладываем на неё наши данные вместо падения в 500 (по образцу
            // PatchFeedFiltersCommandHandler/PatchOnboardingDraftCommandHandler).
            settings = await repository.GetByUserIdTrackedAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"PrivacySettings for user {request.UserId} not found after a concurrent-creation conflict.");

            ApplyPatch(settings, request);
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await repository.SaveChangesAsync(cancellationToken);
        }

        return PrivacySettingsDefaults.ToResult(settings);
    }

    private static void ApplyPatch(PrivacySettings settings, PatchPrivacySettingsCommand request)
    {
        if (request.BlockIncomingMessages is { } blockIncomingMessages)
        {
            settings.BlockIncomingMessages = blockIncomingMessages;
        }

        if (request.HideDistance is { } hideDistance)
        {
            settings.HideDistance = hideDistance;
        }

        if (request.HideAge is { } hideAge)
        {
            settings.HideAge = hideAge;
        }

        if (request.ShowLastActive is { } showLastActive)
        {
            settings.ShowLastActive = showLastActive;
        }

        if (request.InvisibleMode is { } invisibleMode)
        {
            settings.InvisibleMode = invisibleMode;
        }
    }
}
