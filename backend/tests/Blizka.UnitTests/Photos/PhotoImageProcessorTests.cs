using Blizka.App.Photos;
using FluentValidation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Blizka.UnitTests.Photos;

public sealed class PhotoImageProcessorTests
{
    [Fact(DisplayName = "КОГДА в изображении есть EXIF-ориентация ТОГДА оригинал переориентируется, а EXIF/ICC/XMP снимаются")]
    public void Process_reorients_and_strips_metadata()
    {
        using var source = CreateJpegWithExifOrientation(width: 400, height: 200, orientation: 6);

        var result = PhotoImageProcessor.Process(source);

        using var original = Image.Load(result.OriginalBytes);
        // Оригинал был 400x200 с EXIF-ориентацией "повернуть на 90°" — после AutoOrient размеры должны поменяться местами.
        Assert.Equal(200, original.Width);
        Assert.Equal(400, original.Height);
        Assert.Null(original.Metadata.ExifProfile);
        Assert.Null(original.Metadata.IccProfile);
        Assert.Null(original.Metadata.XmpProfile);
    }

    [Fact(DisplayName = "КОГДА фото обрабатывается ТОГДА thumbnail не превышает 150px, а medium — 600px по большей стороне")]
    public void Process_generates_thumbnail_and_medium_within_their_max_dimensions()
    {
        using var source = CreateJpeg(width: 1200, height: 800);

        var result = PhotoImageProcessor.Process(source);

        using var thumbnail = Image.Load(result.ThumbnailBytes);
        using var medium = Image.Load(result.MediumBytes);
        Assert.Equal(150, thumbnail.Width);
        Assert.Equal(100, thumbnail.Height);
        Assert.Equal(600, medium.Width);
        Assert.Equal(400, medium.Height);
    }

    [Fact(DisplayName = "КОГДА оригинал в формате JPEG ТОГДА OriginalContentType/OriginalExtension соответствуют JPEG")]
    public void Process_reports_the_original_format()
    {
        using var source = CreateJpeg(width: 100, height: 100);

        var result = PhotoImageProcessor.Process(source);

        Assert.Equal("image/jpeg", result.OriginalContentType);
        Assert.Equal("jpg", result.OriginalExtension);
    }

    [Fact(DisplayName = "КОГДА файл не является изображением ТОГДА выбрасывается ValidationException")]
    public void Process_throws_ValidationException_for_garbage_bytes()
    {
        using var garbage = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        Assert.Throws<ValidationException>(() => PhotoImageProcessor.Process(garbage));
    }

    [Fact(DisplayName = "КОГДА формат распознан, но не входит в JPG/PNG/WEBP (например BMP) ТОГДА выбрасывается ValidationException")]
    public void Process_throws_ValidationException_for_an_unsupported_but_recognized_format()
    {
        using var bmp = new MemoryStream();
        using (var image = new Image<Rgba32>(10, 10))
        {
            image.Save(bmp, new BmpEncoder());
        }

        bmp.Position = 0;

        Assert.Throws<ValidationException>(() => PhotoImageProcessor.Process(bmp));
    }

    [Fact(DisplayName = "КОГДА фото размывается ТОГДА результат — валидный JPEG того же размера")]
    public void Blur_returns_a_valid_jpeg_of_the_same_dimensions()
    {
        using var source = CreateJpeg(width: 150, height: 100);
        var sourceBytes = source.ToArray();

        var blurredBytes = PhotoImageProcessor.Blur(sourceBytes);

        using var blurred = Image.Load(blurredBytes);
        Assert.Equal(150, blurred.Width);
        Assert.Equal(100, blurred.Height);
    }

    [Theory(DisplayName = "КОГДА Content-Type один из image/jpeg, image/png, image/webp ТОГДА IsSupportedContentType = true")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/JPEG")]
    public void IsSupportedContentType_accepts_the_three_supported_formats(string contentType) =>
        Assert.True(PhotoImageProcessor.IsSupportedContentType(contentType));

    [Fact(DisplayName = "КОГДА Content-Type не из поддерживаемого набора ТОГДА IsSupportedContentType = false")]
    public void IsSupportedContentType_rejects_unknown_content_type() =>
        Assert.False(PhotoImageProcessor.IsSupportedContentType("image/gif"));

    private static MemoryStream CreateJpeg(int width, int height)
    {
        var buffer = new MemoryStream();
        using (var image = new Image<Rgba32>(width, height))
        {
            image.Save(buffer, new JpegEncoder());
        }

        buffer.Position = 0;
        return buffer;
    }

    private static MemoryStream CreateJpegWithExifOrientation(int width, int height, ushort orientation)
    {
        var buffer = new MemoryStream();
        using (var image = new Image<Rgba32>(width, height))
        {
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
            image.Save(buffer, new JpegEncoder());
        }

        buffer.Position = 0;
        return buffer;
    }
}
