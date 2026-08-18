using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blizka.App.Telegram;

/// <summary>
/// Validates the <c>initData</c> string a Telegram Mini App client sends via the
/// <c>X-Telegram-InitData</c> header (T-1.1, backend-spec §Telegram auth): HMAC-SHA256 signature
/// check against the bot token, then a 5-minute <c>auth_date</c> freshness check.
/// Pure/stateless so it can be unit-tested without an HTTP pipeline.
/// </summary>
public static class TelegramInitDataValidator
{
    private static readonly byte[] WebAppDataKey = Encoding.UTF8.GetBytes("WebAppData");
    private static readonly TimeSpan MaxAuthDateAge = TimeSpan.FromMinutes(5);

    public static bool TryValidate(
        string initData,
        string botToken,
        DateTimeOffset now,
        out TelegramInitData? data,
        out string? failureReason)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(initData))
        {
            failureReason = "initData is empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            failureReason = "bot token is not configured";
            return false;
        }

        var fields = ParseQueryString(initData);

        if (!fields.TryGetValue("hash", out var receivedHash) || string.IsNullOrEmpty(receivedHash))
        {
            failureReason = "hash field is missing";
            return false;
        }

        if (!VerifySignature(fields, receivedHash, botToken))
        {
            failureReason = "hash does not match computed signature";
            return false;
        }

        if (!fields.TryGetValue("auth_date", out var authDateRaw) ||
            !long.TryParse(authDateRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var authDateUnix))
        {
            failureReason = "auth_date field is missing or invalid";
            return false;
        }

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        if (now - authDate > MaxAuthDateAge)
        {
            failureReason = "auth_date is older than 5 minutes";
            return false;
        }

        if (!fields.TryGetValue("user", out var userJson) || string.IsNullOrEmpty(userJson))
        {
            failureReason = "user field is missing";
            return false;
        }

        TelegramUserPayload? user;
        try
        {
            user = JsonSerializer.Deserialize<TelegramUserPayload>(userJson);
        }
        catch (JsonException)
        {
            failureReason = "user field is not valid JSON";
            return false;
        }

        if (user is null || user.Id == 0)
        {
            failureReason = "user.id is missing";
            return false;
        }

        data = new TelegramInitData(
            user.Id,
            user.FirstName ?? string.Empty,
            user.LastName,
            user.Username,
            user.PhotoUrl,
            user.LanguageCode,
            authDate);

        failureReason = null;
        return true;
    }

    private static bool VerifySignature(IReadOnlyDictionary<string, string> fields, string receivedHash, string botToken)
    {
        var dataCheckString = string.Join(
            '\n',
            fields
                .Where(field => field.Key != "hash")
                .OrderBy(field => field.Key, StringComparer.Ordinal)
                .Select(field => $"{field.Key}={field.Value}"));

        var secretKey = HMACSHA256.HashData(WebAppDataKey, Encoding.UTF8.GetBytes(botToken));
        var computedHashBytes = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        return CryptographicOperations.FixedTimeEquals(
            computedHashBytes,
            Convert.FromHexString(IsHex(receivedHash) ? receivedHash : string.Empty));
    }

    private static bool IsHex(string value) =>
        value.Length % 2 == 0 && value.All(Uri.IsHexDigit);

    private static Dictionary<string, string> ParseQueryString(string initData)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separatorIndex]);
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private sealed class TelegramUserPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }
    }
}
