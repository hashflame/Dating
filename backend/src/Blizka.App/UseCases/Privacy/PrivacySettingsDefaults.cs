using Blizka.App.Domain.Entities;

namespace Blizka.App.UseCases.Privacy;

/// <summary>
/// Значения по умолчанию для пользователя, у которого ещё нет строки в <c>PrivacySettings</c> (строка создаётся
/// лениво при первом PATCH, а не при регистрации) — используются и на чтении (GET/лента/хаб мэтча), и как
/// стартовое состояние новой строки при первом PATCH.
/// </summary>
public static class PrivacySettingsDefaults
{
    public static readonly PrivacySettingsResult Result = new(
        BlockIncomingMessages: false,
        HideDistance: false,
        HideAge: false,
        ShowLastActive: true,
        InvisibleMode: false);

    public static PrivacySettingsResult ToResult(PrivacySettings? settings) => settings is null
        ? Result
        : new PrivacySettingsResult(
            settings.BlockIncomingMessages,
            settings.HideDistance,
            settings.HideAge,
            settings.ShowLastActive,
            settings.InvisibleMode);
}
