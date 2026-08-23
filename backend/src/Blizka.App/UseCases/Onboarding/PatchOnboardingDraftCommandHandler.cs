using System.Text.Json;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Сохраняет данные одного шага онбординга в черновик пользователя (T-2.1): валидирует шаг,
/// накладывает его данные на уже накопленные и сдвигает отметку прогресса вперёд, если нужно.
/// </summary>
public sealed class PatchOnboardingDraftCommandHandler(
    IOnboardingDraftRepository draftRepository,
    IUserRepository userRepository,
    IValidator<OnboardingStep1Data> step1Validator,
    IValidator<OnboardingStep2Data> step2Validator,
    IValidator<OnboardingStep3Data> step3Validator)
    : IRequestHandler<PatchOnboardingDraftCommand, OnboardingDraftResult>
{
    public async Task<OnboardingDraftResult> Handle(PatchOnboardingDraftCommand request, CancellationToken cancellationToken)
    {
        var normalizedStepData = await ValidateAndNormalizeAsync(request.Step, request.Data, cancellationToken);

        var draft = await draftRepository.GetAsync(request.UserId, cancellationToken);
        var isNewDraft = draft is null;
        draft ??= new OnboardingDraft { UserId = request.UserId };

        ApplyStepData(draft, request.Step, normalizedStepData);

        if (isNewDraft)
        {
            await draftRepository.AddAsync(draft, cancellationToken);

            // Первый PATCH черновика (spec 002, B8) — переводит пользователя из New в Onboarding.
            // Флашится тем же draftRepository.SaveChangesAsync ниже: репозитории шарят один DbContext.
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

            if (user.Status == UserStatus.New)
            {
                user.Status = UserStatus.Onboarding;
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        try
        {
            await draftRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentOnboardingDraftCreationException) when (isNewDraft)
        {
            // Параллельный PATCH того же пользователя успел создать черновик первым — подхватываем
            // уже созданную запись и накладываем на неё наши данные шага вместо падения в 500.
            draft = await draftRepository.GetAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"OnboardingDraft for user {request.UserId} not found after a concurrent-creation conflict.");

            ApplyStepData(draft, request.Step, normalizedStepData);
            await draftRepository.SaveChangesAsync(cancellationToken);
        }

        return new OnboardingDraftResult(draft.Step, JsonSerializer.Deserialize<JsonElement>(draft.DataJson));
    }

    private static void ApplyStepData(OnboardingDraft draft, int step, JsonElement normalizedStepData)
    {
        var accumulated = OnboardingDraftJson.ParseStoredData(draft.DataJson);
        draft.DataJson = OnboardingDraftJson.Merge(accumulated, normalizedStepData);
        draft.Step = Math.Max(draft.Step, step);
        draft.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task<JsonElement> ValidateAndNormalizeAsync(int step, JsonElement data, CancellationToken cancellationToken) =>
        step switch
        {
            1 => await NormalizeAsync(data, step1Validator, cancellationToken),
            2 => await NormalizeAsync(data, step2Validator, cancellationToken),
            3 => await NormalizeAsync(data, step3Validator, cancellationToken),
            _ => throw new ValidationException(
                [new ValidationFailure(nameof(PatchOnboardingDraftCommand.Step), $"Unsupported onboarding step {step}.")]),
        };

    private static async Task<JsonElement> NormalizeAsync<T>(JsonElement data, IValidator<T> validator, CancellationToken cancellationToken)
    {
        var stepData = Deserialize<T>(data);
        await validator.ValidateAndThrowAsync(stepData, cancellationToken);
        return JsonSerializer.SerializeToElement(stepData, OnboardingDraftJson.Options);
    }

    private static T Deserialize<T>(JsonElement data)
    {
        try
        {
            return data.Deserialize<T>(OnboardingDraftJson.Options)
                ?? throw new ValidationException([new ValidationFailure("data", "Step data must not be empty.")]);
        }
        catch (JsonException ex)
        {
            throw new ValidationException([new ValidationFailure("data", $"Step data has an invalid shape: {ex.Message}")]);
        }
    }
}
