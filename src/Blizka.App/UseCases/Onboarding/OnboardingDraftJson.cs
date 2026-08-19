using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Общие настройки сериализации для JSON-полей шагов онбординга (camelCase-имена свойств и enum'ы как
/// строки, как в HTTP-контракте) и слияние накопленных данных черновика с данными нового шага.
/// </summary>
internal static class OnboardingDraftJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    /// <summary>Парсит JSON-текст черновика (или "{}" для нового пользователя) в изменяемый объект.</summary>
    public static JsonObject ParseStoredData(string dataJson) =>
        string.IsNullOrWhiteSpace(dataJson)
            ? []
            : JsonNode.Parse(dataJson)?.AsObject() ?? [];

    /// <summary>Перекладывает поля нормализованных данных шага поверх накопленного объекта — поля других шагов не трогает.</summary>
    public static string Merge(JsonObject accumulated, JsonElement stepData)
    {
        foreach (var property in stepData.EnumerateObject())
        {
            accumulated[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        return accumulated.ToJsonString();
    }
}
