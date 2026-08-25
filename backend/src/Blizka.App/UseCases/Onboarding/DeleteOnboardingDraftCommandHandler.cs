using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Сбрасывает онбординг пользователя: удаляет черновик, возвращает <c>Status</c> в <see cref="UserStatus.New"/>
/// и очищает его собственные свайпы, чтобы тот же telegramId можно было провести через регистрацию и ленту
/// заново на нестабильном стенде без заведения нового тестового пользователя каждый прогон (см.
/// <c>OnboardingController.DeleteDraft</c>) — сознательно не трогает уже начисленные зорки/фото/интересы/
/// UserFilter/мэтчи и чужие свайпы на этого пользователя, это debug-утилита, а не полное удаление аккаунта
/// (для него есть отдельный будущий <c>DELETE /api/users/me/account</c>, T-16.1).
/// </summary>
public sealed class DeleteOnboardingDraftCommandHandler(
    IOnboardingDraftRepository draftRepository,
    IUserRepository userRepository,
    ISwipeRepository swipeRepository)
    : IRequestHandler<DeleteOnboardingDraftCommand>
{
    public async Task Handle(DeleteOnboardingDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await draftRepository.GetAsync(request.UserId, cancellationToken);
        if (draft is not null)
        {
            draftRepository.Remove(draft);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is not null && user.Status is UserStatus.Onboarding or UserStatus.Active)
        {
            user.Status = UserStatus.New;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await swipeRepository.RemoveAllByUserAsync(request.UserId, cancellationToken);

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            // По образцу остальных хендлеров, мутирующих User (UndoSwipeCommandHandler, SwipeCommandHandler,
            // CompleteOnboardingCommandHandler и т.д.) — без этого конфликт xmin улетел бы необработанным
            // исключением в BlizkaExceptionHandler и превратился бы в 500 вместо понятного клиенту 409.
            throw new OnboardingDraftResetConflictException(request.UserId, ex);
        }
    }
}
