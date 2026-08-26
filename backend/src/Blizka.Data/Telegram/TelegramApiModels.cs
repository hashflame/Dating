using System.Text.Json.Serialization;

namespace Blizka.Data.Telegram;

/// <summary>Конверт ответа Telegram Bot API — общий для всех методов (<c>ok</c> + либо <c>result</c>, либо описание ошибки).</summary>
internal sealed record TelegramApiResponse<TResult>(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] TResult? Result,
    [property: JsonPropertyName("error_code")] int? ErrorCode,
    [property: JsonPropertyName("description")] string? Description);

internal sealed record SendMessagePayload(
    [property: JsonPropertyName("chat_id")] long ChatId,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("parse_mode")] string? ParseMode);

internal sealed record CreateInvoiceLinkPayload(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("provider_token")] string ProviderToken,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("prices")] IReadOnlyList<LabeledPricePayload> Prices);

internal sealed record LabeledPricePayload(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("amount")] int Amount);

internal sealed record UserProfilePhotosResult(
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("photos")] IReadOnlyList<IReadOnlyList<PhotoSize>> Photos);

/// <summary>Один размер одной фотографии профиля — Telegram отдаёт несколько размеров на фото, по возрастанию.</summary>
internal sealed record PhotoSize(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

internal sealed record FileResult(
    [property: JsonPropertyName("file_path")] string? FilePath);
