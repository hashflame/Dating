namespace Blizka.Api.ErrorHandling;

/// <summary>Языки, на которые локализуются сообщения об ошибках. Отражает JWT-claim `locale` из T-1.1.</summary>
public enum ApiLocale
{
    Ru,
    Be,
    En,
}

public static class ApiLocaleParser
{
    public const ApiLocale Default = ApiLocale.Ru;

    public static bool TryParse(string? value, out ApiLocale locale)
    {
        // Принимает и голые коды ("en"), и языковые теги ("en-US") — важен только primary subtag.
        var primarySubtag = value?.Trim().ToLowerInvariant().Split('-')[0];

        switch (primarySubtag)
        {
            case "ru":
                locale = ApiLocale.Ru;
                return true;
            case "be":
                locale = ApiLocale.Be;
                return true;
            case "en":
                locale = ApiLocale.En;
                return true;
            default:
                locale = Default;
                return false;
        }
    }
}
