using Blizka.App.Photos;
using FluentValidation;

namespace Blizka.App.UseCases.Photos;

/// <summary>Проверки на уровне запроса (до декодирования файла) — формат из заголовка и лимит размера в 10MB.</summary>
public sealed class UploadPhotoCommandValidator : AbstractValidator<UploadPhotoCommand>
{
    public UploadPhotoCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(PhotoImageProcessor.IsSupportedContentType)
            .WithMessage("Поддерживаются только форматы JPG, PNG и WEBP.");

        RuleFor(x => x.ContentLength)
            .GreaterThan(0)
            .WithMessage("Файл пуст.")
            .LessThanOrEqualTo(PhotoImageProcessor.MaxContentLengthBytes)
            .WithMessage("Максимальный размер файла — 10MB.");
    }
}
