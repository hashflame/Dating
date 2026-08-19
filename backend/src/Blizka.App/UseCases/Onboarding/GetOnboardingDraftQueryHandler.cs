using System.Text.Json;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>Возвращает текущий шаг и накопленные данные черновика онбординга (T-2.1); для пользователя без черновика — пустое состояние.</summary>
public sealed class GetOnboardingDraftQueryHandler(IOnboardingDraftRepository draftRepository)
    : IRequestHandler<GetOnboardingDraftQuery, OnboardingDraftResult>
{
    public async Task<OnboardingDraftResult> Handle(GetOnboardingDraftQuery request, CancellationToken cancellationToken)
    {
        var draft = await draftRepository.GetAsync(request.UserId, cancellationToken);

        return draft is null
            ? new OnboardingDraftResult(0, JsonSerializer.Deserialize<JsonElement>("{}"))
            : new OnboardingDraftResult(draft.Step, JsonSerializer.Deserialize<JsonElement>(draft.DataJson));
    }
}
