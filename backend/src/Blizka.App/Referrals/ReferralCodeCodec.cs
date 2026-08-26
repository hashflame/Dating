namespace Blizka.App.Referrals;

/// <summary>
/// Кодирует/декодирует реферальный код (T-20.1) как base64url представление <see cref="Guid"/> реферера —
/// без отдельного персистентного поля/таблицы под "код пользователя": код полностью восстанавливается
/// обратно в ReferrerUserId, а хранится (в <c>Referral.Code</c>) только как аудиторская копия использованного значения.
/// </summary>
public static class ReferralCodeCodec
{
    public const string StartParamPrefix = "ref_";

    public static string Encode(Guid referrerUserId) =>
        Convert.ToBase64String(referrerUserId.ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static bool TryDecode(string code, out Guid referrerUserId)
    {
        referrerUserId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var base64 = code.Replace('-', '+').Replace('_', '/');
        var padded = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');

        try
        {
            var bytes = Convert.FromBase64String(padded);
            if (bytes.Length != 16)
            {
                return false;
            }

            referrerUserId = new Guid(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Извлекает код из <c>start_param</c> Telegram WebApp (формат <c>ref_{code}</c>) и декодирует его в id реферера.</summary>
    public static bool TryDecodeStartParam(string? startParam, out Guid referrerUserId)
    {
        referrerUserId = Guid.Empty;

        if (string.IsNullOrEmpty(startParam) || !startParam.StartsWith(StartParamPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return TryDecode(startParam[StartParamPrefix.Length..], out referrerUserId);
    }
}
