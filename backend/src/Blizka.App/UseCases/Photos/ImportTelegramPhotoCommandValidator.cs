using FluentValidation;

namespace Blizka.App.UseCases.Photos;

/// <summary>
/// Ограничивает <c>photoUrl</c> доменом Telegram CDN — эндпоинт скачивает файл по адресу, присланному клиентом,
/// и без ограничения хоста превратился бы в SSRF (сервер можно было бы заставить обратиться к любому URL,
/// включая внутренние адреса инфраструктуры).
/// </summary>
public sealed class ImportTelegramPhotoCommandValidator : AbstractValidator<ImportTelegramPhotoCommand>
{
    private static readonly string[] AllowedHosts = ["t.me"];

    public ImportTelegramPhotoCommandValidator()
    {
        RuleFor(x => x.PhotoUrl)
            .NotEmpty()
            .Must(BeAnAllowedTelegramCdnUrl)
            .WithMessage("photoUrl должен быть ссылкой на Telegram CDN (https://t.me/...).");
    }

    private static bool BeAnAllowedTelegramCdnUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
}
