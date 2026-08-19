using System.Security.Cryptography;
using System.Text;
using Blizka.App.Telegram;

namespace Blizka.UnitTests.Telegram;

public sealed class TelegramInitDataValidatorTests
{
    private const string BotToken = "123456:TEST-BOT-TOKEN";

    [Fact(DisplayName = "КОГДА payload подписан корректно ТОГДА валидация проходит успешно")]
    public void TryValidate_accepts_a_correctly_signed_payload()
    {
        var now = DateTimeOffset.UtcNow;
        var initData = BuildSignedInitData(BotToken, now, telegramId: 42, firstName: "Ann", lastName: "K", languageCode: "ru-RU");

        var valid = TelegramInitDataValidator.TryValidate(initData, BotToken, now, out var data, out var failureReason);

        Assert.True(valid);
        Assert.Null(failureReason);
        Assert.NotNull(data);
        Assert.Equal(42, data!.TelegramId);
        Assert.Equal("Ann", data.FirstName);
        Assert.Equal("K", data.LastName);
        Assert.Equal("ru-RU", data.LanguageCode);
    }

    [Fact(DisplayName = "КОГДА hash подделан ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_a_tampered_hash()
    {
        var now = DateTimeOffset.UtcNow;
        var initData = BuildSignedInitData(BotToken, now, telegramId: 42, firstName: "Ann");
        var tampered = initData.Replace("Ann", "Eve");

        var valid = TelegramInitDataValidator.TryValidate(tampered, BotToken, now, out var data, out var failureReason);

        Assert.False(valid);
        Assert.Null(data);
        Assert.NotNull(failureReason);
    }

    [Fact(DisplayName = "КОГДА указан неверный токен бота ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_wrong_bot_token()
    {
        var now = DateTimeOffset.UtcNow;
        var initData = BuildSignedInitData(BotToken, now, telegramId: 42, firstName: "Ann");

        var valid = TelegramInitDataValidator.TryValidate(initData, "999999:OTHER-TOKEN", now, out _, out var failureReason);

        Assert.False(valid);
        Assert.NotNull(failureReason);
    }

    [Fact(DisplayName = "КОГДА auth_date старше пяти минут ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_auth_date_older_than_five_minutes()
    {
        var authDate = DateTimeOffset.UtcNow.AddMinutes(-6);
        var initData = BuildSignedInitData(BotToken, authDate, telegramId: 42, firstName: "Ann");

        var valid = TelegramInitDataValidator.TryValidate(initData, BotToken, DateTimeOffset.UtcNow, out _, out var failureReason);

        Assert.False(valid);
        Assert.Contains("auth_date", failureReason);
    }

    [Fact(DisplayName = "КОГДА auth_date чуть меньше пяти минут ТОГДА валидация проходит успешно")]
    public void TryValidate_accepts_auth_date_just_under_five_minutes_old()
    {
        var authDate = DateTimeOffset.UtcNow.AddMinutes(-4);
        var initData = BuildSignedInitData(BotToken, authDate, telegramId: 42, firstName: "Ann");

        var valid = TelegramInitDataValidator.TryValidate(initData, BotToken, DateTimeOffset.UtcNow, out _, out _);

        Assert.True(valid);
    }

    [Fact(DisplayName = "КОГДА hash отсутствует ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_missing_hash()
    {
        var now = DateTimeOffset.UtcNow;
        var initData = $"auth_date={now.ToUnixTimeSeconds()}&user=%7B%22id%22%3A42%2C%22first_name%22%3A%22Ann%22%7D";

        var valid = TelegramInitDataValidator.TryValidate(initData, BotToken, now, out _, out var failureReason);

        Assert.False(valid);
        Assert.Contains("hash", failureReason);
    }

    [Fact(DisplayName = "КОГДА initData пустая ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_empty_initData()
    {
        var valid = TelegramInitDataValidator.TryValidate(string.Empty, BotToken, DateTimeOffset.UtcNow, out _, out var failureReason);

        Assert.False(valid);
        Assert.NotNull(failureReason);
    }

    [Fact(DisplayName = "КОГДА токен бота пустой ТОГДА валидация отклоняется")]
    public void TryValidate_rejects_empty_bot_token()
    {
        var now = DateTimeOffset.UtcNow;
        var initData = BuildSignedInitData(BotToken, now, telegramId: 42, firstName: "Ann");

        var valid = TelegramInitDataValidator.TryValidate(initData, string.Empty, now, out _, out var failureReason);

        Assert.False(valid);
        Assert.NotNull(failureReason);
    }

    /// <summary>Строит initData в реальном формате Telegram, подписанную так же, как это делают клиенты Telegram — независимый эталон для проверки валидатора.</summary>
    private static string BuildSignedInitData(
        string botToken,
        DateTimeOffset authDate,
        long telegramId,
        string firstName,
        string? lastName = null,
        string? languageCode = null)
    {
        var userJson = $$"""{"id":{{telegramId}},"first_name":"{{firstName}}"{{(lastName is null ? "" : $",\"last_name\":\"{lastName}\"")}}{{(languageCode is null ? "" : $",\"language_code\":\"{languageCode}\"")}}}""";

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAH1234567890",
            ["user"] = userJson,
        };

        var dataCheckString = string.Join('\n', fields.Select(f => $"{f.Key}={f.Value}"));

        var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        var queryFields = fields.ToDictionary(f => f.Key, f => f.Value);
        queryFields["hash"] = hash;

        return string.Join('&', queryFields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));
    }
}
