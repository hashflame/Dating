using Blizka.Api.Onboarding;
using Blizka.App.UseCases.Onboarding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Blizka.Host.OpenApi;

/// <summary>
/// <see cref="PatchOnboardingDraftRequest.Data"/> биндится как сырой <c>JsonElement</c> — форма шага не
/// типизирована на уровне DTO, её парсит и валидирует <c>PatchOnboardingDraftCommandHandler.ValidateAndNormalizeAsync</c>
/// по номеру шага уже после десериализации. По умолчанию это даёт в OpenAPI пустую схему (<c>{}</c> — "любой
/// JSON"), и клиент, сгенерированный по спеке, не может провалидировать тело до отправки — только после 400.
/// Подменяем схему поля <c>data</c> на <c>oneOf</c> трёх реальных форм шага, чтобы её было видно в спеке.
/// </summary>
public sealed class OnboardingDraftDataSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(PatchOnboardingDraftRequest)
            || schema.Properties is null
            || !schema.Properties.ContainsKey("data"))
        {
            return;
        }

        var step1Schema = await context.GetOrCreateSchemaAsync(typeof(OnboardingStep1Data), cancellationToken: cancellationToken);
        var step2Schema = await context.GetOrCreateSchemaAsync(typeof(OnboardingStep2Data), cancellationToken: cancellationToken);
        var step3Schema = await context.GetOrCreateSchemaAsync(typeof(OnboardingStep3Data), cancellationToken: cancellationToken);

        // Заменяем весь элемент словаря, а не мутируем то, что в нём лежало: по умолчанию data — это $ref на
        // общую именованную схему JsonElement, разделяемую со всеми остальными JsonElement-полями API
        // (например, OnboardingDraftResponse.Data) — мутация её содержимого задела бы их все.
        schema.Properties["data"] = new OpenApiSchema
        {
            OneOf = [step1Schema, step2Schema, step3Schema],
            Description =
                "Форма зависит от step: 1 -> данные шага 1 (имя/дата рождения/пол), " +
                "2 -> данные шага 2 (предпочтения по полу/возрасту/цели), 3 -> данные шага 3 (город/координаты).",
        };
    }
}
