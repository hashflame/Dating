using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Sparks;

/// <summary>
/// Локализованное название способа заработать зорки (T-8.1, баг «earnOptions без label») — по образцу
/// <see cref="Onboarding.NextRewardHintCatalog"/>: принимает локаль обычной строкой ("ru"/"be"/"en"), не
/// <c>ApiLocale</c> (тот определён в Blizka.Api, а App-слой на него ссылаться не может).
/// </summary>
internal static class SparkEarnOptionLabelCatalog
{
    private static readonly IReadOnlyDictionary<SparkTransactionType, (string Ru, string Be, string En)> Labels =
        new Dictionary<SparkTransactionType, (string Ru, string Be, string En)>
        {
            [SparkTransactionType.RegistrationBonus] = ("Бонус за регистрацию", "Бонус за рэгістрацыю", "Registration bonus"),
            [SparkTransactionType.ProfileCompletion] = ("Заполнение профиля", "Запаўненне профілю", "Complete your profile"),
            [SparkTransactionType.Verification] = ("Верификация по селфи", "Верыфікацыя па сэлфі", "Selfie verification"),
            [SparkTransactionType.Referral] = ("Пригласить друга", "Запрасіць сябра", "Invite a friend"),
            [SparkTransactionType.IdeaSubmission] = ("Предложить идею", "Прапанаваць ідэю", "Submit an idea"),
            [SparkTransactionType.IdeaImplemented] = ("Идея реализована", "Ідэя рэалізавана", "Idea implemented"),
        };

    public static string Resolve(SparkTransactionType type, string locale) => (Labels.TryGetValue(type, out var label), locale) switch
    {
        (true, "be") => label.Be,
        (true, "en") => label.En,
        (true, _) => label.Ru,
        _ => string.Empty,
    };
}
