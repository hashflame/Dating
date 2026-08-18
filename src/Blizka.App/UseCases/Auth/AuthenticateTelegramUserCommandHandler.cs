using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Telegram;
using MediatR;

namespace Blizka.App.UseCases.Auth;

/// <summary>
/// Создаёт или обновляет <see cref="User"/> на основе уже HMAC-верифицированного Telegram initData
/// и выдаёт сессионный JWT (T-1.1).
/// </summary>
public sealed class AuthenticateTelegramUserCommandHandler(
    IUserRepository userRepository,
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
                Status = UserStatus.New,
                Name = BuildName(initData),
                Locale = ResolveLocale(initData.LanguageCode),
                CreatedAt = now,
                UpdatedAt = now,
                LastActiveAt = now,
            };

            await userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            user.LastActiveAt = now;
            user.UpdatedAt = now;
        }

        if (user.Status == UserStatus.Banned)
        {
            throw new UserBannedException(user.Id);
        }

        if (user.Status == UserStatus.Deleted)
        {
            throw new UserDeletedException(user.Id);
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
                throw new UserBannedException(user.Id);
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
            user.Status.ToString(),
            isNewUser);
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
