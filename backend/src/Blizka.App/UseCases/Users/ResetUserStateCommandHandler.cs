using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Onboarding;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Обрабатывает <see cref="ResetUserStateCommand"/>: чистит лайки/дизлайки (обе стороны), мэтчи, фото,
/// интересы, предпочтения на свидания и необязательные поля профиля, обнуляет пороговые бонусы
/// ProfileCompleteness и возвращает баланс зорок к тому, что было сразу после онбординга — регистрационный
/// бонус, если он уже начислен, иначе 0 (пороговые бонусы за заполненность на этом этапе ещё не
/// начисляются, T-2.3: 35% ниже первого порога 60%). Файлы фото в MinIO не трогает — только записи
/// <see cref="Domain.Entities.Photo"/> в БД (по решению пользователя: осиротевшие файлы в сторидже не
/// критичны для dev-инструмента, как и в уже существующем <see cref="Onboarding.DeleteOnboardingDraftCommandHandler"/>).
/// </summary>
public sealed class ResetUserStateCommandHandler(
    IUserRepository userRepository,
    ISwipeRepository swipeRepository,
    IMatchRepository matchRepository,
    IPhotoRepository photoRepository,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<ResetUserStateCommand>
{
    public async Task Handle(ResetUserStateCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

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

        // RegistrationBonusAwardedAt намеренно не трогаем — он уже был начислен при завершении
        // онбординга (CompleteOnboardingCommandHandler), а не после; сброс к "состоянию после онбординга"
        // не должен позволять начислить его повторно через какой-нибудь будущий путь.
        user.CompletenessBonus60AwardedAt = null;
        user.CompletenessBonus80AwardedAt = null;
        user.CompletenessBonus100AwardedAt = null;
        user.ProfileCompleteness = ProfileCompletenessCalculator.BaseCompleteness;

        user.SparksBalance = user.RegistrationBonusAwardedAt is null ? 0 : sparksOptions.Value.RegistrationBonusAmount;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            throw new ProfileUpdateConflictException(request.UserId, ex);
        }
    }
}
