using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Referrals;
using Blizka.App.Sparks;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Onboarding;
using Blizka.App.UseCases.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Auth;

/// <summary>
/// Создаёт или обновляет <see cref="User"/> на основе уже HMAC-верифицированного Telegram initData
/// и выдаёт сессионный JWT (T-1.1). При первой регистрации по реферальной ссылке (<c>start_param=ref_{code}</c>,
/// T-20.1) заводит запись <see cref="Referral"/> — бонус рефереру начисляется позже, при завершении
/// онбординга приглашённым (см. <c>CompleteOnboardingCommandHandler</c>). Повторный вход на ранее удалённый
/// аккаунт (<c>Status = Deleted</c>) не отдаёт 410 навсегда — поднимает его в состояние нового пользователя
/// (<c>Status = New</c>) через <see cref="UserStateResetter"/>, тот же переиспользуемый сброс, что и
/// dev-инструмент <c>ResetUserStateCommandHandler</c> (тикет ClickUp: удаление аккаунта — «я ухожу», а не
/// бан навсегда; удалить и вернуться через полгода — обычный сценарий для дейтинга).
/// </summary>
public sealed class AuthenticateTelegramUserCommandHandler(
    IUserRepository userRepository,
    IReferralRepository referralRepository,
    ISwipeRepository swipeRepository,
    IMatchRepository matchRepository,
    IPhotoRepository photoRepository,
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<AuthenticateTelegramUserCommand, AuthenticateTelegramUserResult>
{
    private static readonly string[] SupportedLocales = ["ru", "be", "en"];

    public async Task<AuthenticateTelegramUserResult> Handle(
        AuthenticateTelegramUserCommand request, CancellationToken cancellationToken)
    {
        var initData = request.InitData;
        var now = DateTimeOffset.UtcNow;

        var user = await userRepository.GetByTelegramIdAsync(initData.TelegramId, cancellationToken);
        var isNewUser = user is null;

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = initData.TelegramId,
                TelegramUsername = initData.Username,
                Status = UserStatus.New,
                Name = BuildName(initData),
                Locale = ResolveLocale(initData.LanguageCode),
                CreatedAt = now,
                UpdatedAt = now,
                LastActiveAt = now,
            };

            await userRepository.AddAsync(user, cancellationToken);
            await TryAttributeReferralAsync(user, initData.StartParam, cancellationToken);
        }
        else
        {
            // Telegram username может меняться — обновляем при каждом логине (spec 002, B6).
            user.TelegramUsername = initData.Username;
            user.LastActiveAt = now;
            user.UpdatedAt = now;
        }

        if (user.Status == UserStatus.Banned)
        {
            throw new UserBannedException(user.Id, user.BanReason, user.BannedUntil);
        }

        if (user.Status == UserStatus.Deleted)
        {
            user = await ReviveDeletedAccountAsync(user, now, cancellationToken);
            // Не только сигнал для ответа (isNewUser: true, тикет ClickUp) — переиспользуется ниже как guard
            // для ConcurrentUserCreationException. Безопасно: ревайв никогда не вставляет строку User (только
            // обновляет уже существующую), поэтому TelegramId-race, под который написан этот catch, из этой
            // ветки в принципе не долетит — но если когда-нибудь понадобится развести эти два смысла, здесь
            // граница.
            isNewUser = true;
        }

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserCreationException) when (isNewUser)
        {
            // Параллельный запрос с тем же telegramId успел создать пользователя первым —
            // подхватываем уже созданную запись вместо падения в 500.
            user = await userRepository.GetByTelegramIdAsync(initData.TelegramId, cancellationToken)
                ?? throw new InvalidOperationException($"User with telegramId {initData.TelegramId} not found after a concurrent-creation conflict.");
            isNewUser = false;

            if (user.Status == UserStatus.Banned)
            {
                throw new UserBannedException(user.Id, user.BanReason, user.BannedUntil);
            }

            if (user.Status == UserStatus.Deleted)
            {
                throw new UserDeletedException(user.Id);
            }
        }

        var issuedToken = jwtTokenService.IssueToken(user);

        return new AuthenticateTelegramUserResult(
            issuedToken.Token,
            issuedToken.ExpiresAt,
            user.Id,
            user.Status,
            isNewUser,
            user.Locale);
    }

    // Возвращает "я ухожу", а не "забаньте меня навсегда" — тикет ClickUp: раньше Status = Deleted навсегда
    // отдавал 410. Переиспользует UserStateResetter (та же логика, что и dev-инструмент ResetUserStateCommandHandler),
    // но НЕ трогает RegistrationBonusAwardedAt/CompletenessBonus60/80/100AwardedAt (в отличие от dev-сброса) —
    // иначе цикл "удалил → вошёл → снова заработал" позволял бы бесконечно фармить зорки. Реферальная связь,
    // жалобы и история блокировок тоже не трогаются: удаление аккаунта не должно стирать историю модерации
    // или обнулять уже начисленные рефералы. GetByIdWithProfileDataAsync возвращает тот же трекаемый EF-инстанс
    // (identity resolution в рамках одного DbContext), поэтому уже применённые выше TelegramUsername/LastActiveAt/
    // UpdatedAt не теряются.
    private async Task<User> ReviveDeletedAccountAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var loadedUser = await userRepository.GetByIdWithProfileDataAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} not found while reviving a deleted account.");

        await UserStateResetter.ClearActivityAndOptionalProfileAsync(
            loadedUser, swipeRepository, matchRepository, photoRepository, cancellationToken);

        loadedUser.ProfileCompleteness = ProfileCompletenessCalculator.BaseCompleteness;

        var targetBalance = loadedUser.RegistrationBonusAwardedAt is null ? 0 : sparksOptions.Value.RegistrationBonusAmount;
        var delta = targetBalance - loadedUser.SparksBalance;
        if (delta != 0)
        {
            await sparksService.AdjustAsync(loadedUser, delta, SparkTransactionType.AccountRevival, referenceId: null, cancellationToken);
        }

        loadedUser.Status = UserStatus.New;
        loadedUser.DeletedAt = null;
        loadedUser.UpdatedAt = now;

        return loadedUser;
    }

    // Только на самой первой регистрации (isNewUser) — реферала нельзя переприсвоить задним числом при
    // последующих логинах того же пользователя. Молча игнорируем некорректный/несуществующий код: неверный
    // start_param не должен ронять аутентификацию.
    //
    // Самореферальность одного и того же Telegram-аккаунта структурно невозможна: этот метод вызывается
    // только из ветки isNewUser, т.е. referredUser — гарантированно новый TelegramId, которого не может
    // быть ни у одного существующего пользователя (включая самого referrer'а). Проверка "referrerUserId ==
    // referredUser.Id" здесь была бы фиктивной (referredUser.Id всегда свежий Guid.NewGuid(), совпадение
    // с decoded referrerUserId физически невозможно), поэтому её здесь нет. Самореферальность через ВТОРОЙ
    // Telegram-аккаунт того же человека этот метод не ловит и сознательно не пытается — на уровне Telegram
    // initData нет надёжного сигнала "тот же человек", вопрос анти-фрода вынесен за рамки T-20.1.
    private async Task TryAttributeReferralAsync(User referredUser, string? startParam, CancellationToken cancellationToken)
    {
        if (!ReferralCodeCodec.TryDecodeStartParam(startParam, out var referrerUserId))
        {
            return;
        }

        var referrer = await userRepository.GetByIdAsync(referrerUserId, cancellationToken);
        if (referrer is null)
        {
            return;
        }

        await referralRepository.AddAsync(
            new Referral
            {
                Id = Guid.NewGuid(),
                ReferrerUserId = referrer.Id,
                ReferredUserId = referredUser.Id,
                Code = startParam![ReferralCodeCodec.StartParamPrefix.Length..],
                Status = ReferralStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }

    private static string BuildName(TelegramInitData initData) =>
        string.IsNullOrWhiteSpace(initData.LastName)
            ? initData.FirstName
            : $"{initData.FirstName} {initData.LastName}";

    private static string ResolveLocale(string? languageCode)
    {
        var primary = languageCode?.Split('-')[0].ToLowerInvariant();
        return SupportedLocales.Contains(primary) ? primary! : "ru";
    }
}
