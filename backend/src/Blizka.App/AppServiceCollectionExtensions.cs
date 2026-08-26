using Blizka.App.DataExport;
using Blizka.App.Notifications;
using Blizka.App.Referrals;
using Blizka.App.Sparks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blizka.App;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddAppLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        services.AddOptions<SparksOptions>()
            .Bind(configuration.GetSection(SparksOptions.SectionName))
            .Validate(o => o.SuperlikeCost > 0, "Sparks:SuperlikeCost должен быть положительным.")
            .Validate(o => o.LikesRevealCost > 0, "Sparks:LikesRevealCost должен быть положительным.")
            .Validate(o => o.ContactUnlockCost > 0, "Sparks:ContactUnlockCost должен быть положительным.")
            .Validate(o => o.RegistrationBonusAmount > 0, "Sparks:RegistrationBonusAmount должен быть положительным.")
            .Validate(o => o.ProfileCompletionThresholdBonusAmount > 0, "Sparks:ProfileCompletionThresholdBonusAmount должен быть положительным.")
            .Validate(o => o.VerificationBonusAmount > 0, "Sparks:VerificationBonusAmount должен быть положительным.")
            .Validate(o => o.ReferralBonusAmount > 0, "Sparks:ReferralBonusAmount должен быть положительным.")
            .Validate(o => o.IdeaSubmissionBonusAmount > 0, "Sparks:IdeaSubmissionBonusAmount должен быть положительным.")
            .Validate(o => o.IdeaImplementedBonusAmount > 0, "Sparks:IdeaImplementedBonusAmount должен быть положительным.")
            .ValidateOnStart();
        services.AddScoped<ISparksService, SparksService>();

        services.AddOptions<ReferralOptions>()
            .Bind(configuration.GetSection(ReferralOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BotUsername), "Referral:BotUsername не задан.")
            .ValidateOnStart();

        // Singleton — очередь должна пережить каждый отдельный HTTP-запрос/scope, чтобы уведомление,
        // поставленное в scoped-обработчике, дождалось фонового читателя (Blizka.Host).
        services.AddSingleton<INotificationQueue, NotificationQueue>();
        services.AddScoped<INotificationService, NotificationService>();

        // Singleton по той же причине, что и INotificationQueue выше — должна пережить scoped-обработчик,
        // который поставил запрос в очередь.
        services.AddSingleton<IDataExportQueue, DataExportQueue>();

        return services;
    }
}
