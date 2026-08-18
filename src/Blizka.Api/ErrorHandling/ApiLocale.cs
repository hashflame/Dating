namespace Blizka.Api.ErrorHandling;

/// <summary>Languages error messages are localized into. Mirrors the `locale` JWT claim from T-1.1.</summary>
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
        // Accepts both bare codes ("en") and language tags ("en-US") — only the primary subtag matters.
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
