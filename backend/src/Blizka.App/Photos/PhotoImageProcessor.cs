using FluentValidation;
using FluentValidation.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Blizka.App.Photos;

/// <summary>
/// Декодирует, удаляет EXIF/ICC/XMP-метаданные и генерирует thumbnail (150px)/medium (600px) для фото профиля
/// (T-3.1). Чистая библиотека изображений (как NetTopologySuite — см. CLAUDE.md), без ASP.NET Core/EF Core.
/// </summary>
public static class PhotoImageProcessor
{
    public const int ThumbnailMaxDimension = 150;
    public const int MediumMaxDimension = 600;
    public const long MaxContentLengthBytes = 10 * 1024 * 1024;

    private const int JpegQuality = 85;
    private const int DerivedJpegQuality = 80;

    private static readonly HashSet<string> SupportedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public static bool IsSupportedContentType(string contentType) => SupportedContentTypes.Contains(contentType);

    /// <summary>
    /// Загружает изображение и возвращает три варианта: оригинал (переориентированный по EXIF и без самих
    /// метаданных, в исходном формате), thumbnail и medium (оба — JPEG, независимо от исходного формата, ради
    /// предсказуемого размера и единообразной отдачи клиенту).
    /// </summary>
    public static ProcessedPhoto Process(Stream content)
    {
        content.Position = 0;

        Image image;
        try
        {
            image = Image.Load(content);
        }
        catch (UnknownImageFormatException)
        {
            throw NotAnImage();
        }
        catch (InvalidImageContentException)
        {
            throw NotAnImage();
        }

        using (image)
        {
            var format = image.Metadata.DecodedImageFormat ?? throw NotAnImage();

            if (format is not (JpegFormat or PngFormat or WebpFormat))
            {
                throw new ValidationException(
                    [new ValidationFailure("file", "Поддерживаются только форматы JPG, PNG и WEBP.")]);
            }

            // Данные ориентации (поворот с камеры) хранятся в самом EXIF — если снять профиль до применения
            // поворота, фото окажется "лежащим на боку" у всех, кто снимал вертикально.
            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            var originalBytes = Encode(image, format);
            var thumbnailBytes = EncodeJpeg(Resize(image, ThumbnailMaxDimension));
            var mediumBytes = EncodeJpeg(Resize(image, MediumMaxDimension));

            return new ProcessedPhoto(
                originalBytes,
                ContentTypeFor(format),
                ExtensionFor(format),
                thumbnailBytes,
                mediumBytes);
        }
    }

    private static Image Resize(Image source, int maxDimension) =>
        source.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxDimension, maxDimension),
        }));

    private static byte[] Encode(Image image, IImageFormat format)
    {
        using var buffer = new MemoryStream();
        image.Save(buffer, format is PngFormat
            ? new PngEncoder()
            : format is WebpFormat
                ? new WebpEncoder { Quality = JpegQuality }
                : new JpegEncoder { Quality = JpegQuality });
        return buffer.ToArray();
    }

    private static byte[] EncodeJpeg(Image image)
    {
        using (image)
        {
            using var buffer = new MemoryStream();
            image.Save(buffer, new JpegEncoder { Quality = DerivedJpegQuality });
            return buffer.ToArray();
        }
    }

    private static string ContentTypeFor(IImageFormat format) => format switch
    {
        PngFormat => "image/png",
        WebpFormat => "image/webp",
        _ => "image/jpeg",
    };

    private static string ExtensionFor(IImageFormat format) => format switch
    {
        PngFormat => "png",
        WebpFormat => "webp",
        _ => "jpg",
    };

    private static ValidationException NotAnImage() =>
        new([new ValidationFailure("file", "Файл повреждён или не является изображением.")]);
}

/// <param name="OriginalBytes">Оригинал с удалёнными метаданными, без изменения разрешения.</param>
/// <param name="OriginalContentType">Content-Type оригинала (сохраняет исходный формат файла).</param>
/// <param name="OriginalExtension">Расширение без точки, соответствует <paramref name="OriginalContentType"/>.</param>
/// <param name="ThumbnailBytes">150px, JPEG.</param>
/// <param name="MediumBytes">600px, JPEG.</param>
public sealed record ProcessedPhoto(
    byte[] OriginalBytes,
    string OriginalContentType,
    string OriginalExtension,
    byte[] ThumbnailBytes,
    byte[] MediumBytes);
