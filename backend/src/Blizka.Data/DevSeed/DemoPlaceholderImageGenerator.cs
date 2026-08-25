using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.Data.DevSeed;

/// <summary>
/// Генерирует однотонные JPEG-заглушки для фото демо-анкет (спека 003) — без похода во внешние сервисы
/// (устойчивее и быстрее, чем скачивать placeholder-картинки по сети при каждом пересидировании). Результат
/// прогоняется через тот же <c>UploadPhotoCommand</c>, что и настоящая загрузка (thumbnail/medium/оригинал,
/// заливка в реальный MinIO) — здесь только «сырой» файл, как будто пришедший от клиента.
/// </summary>
public static class DemoPlaceholderImageGenerator
{
    private const int Width = 800;
    private const int Height = 1000;

    /// <summary>Разные, но детерминированные оттенки на пользователя и на фото — чтобы в ленте они визуально отличались.</summary>
    public static byte[] Generate(int userIndex, int photoIndex)
    {
        var hue = (userIndex - 1) * 36f % 360f;
        var lightness = photoIndex == 0 ? 0.55f : 0.45f - (photoIndex * 0.05f);
        var color = ColorFromHsl(hue, 0.55f, Math.Clamp(lightness, 0.2f, 0.6f));

        using var image = new Image<Rgba32>(Width, Height, color);
        using var buffer = new MemoryStream();
        image.Save(buffer, new JpegEncoder { Quality = 80 });
        return buffer.ToArray();
    }

    private static Rgba32 ColorFromHsl(float hue, float saturation, float lightness)
    {
        var c = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        var x = c * (1 - Math.Abs((hue / 60f % 2) - 1));
        var m = lightness - (c / 2);

        var (r, g, b) = hue switch
        {
            < 60 => (c, x, 0f),
            < 120 => (x, c, 0f),
            < 180 => (0f, c, x),
            < 240 => (0f, x, c),
            < 300 => (x, 0f, c),
            _ => (c, 0f, x),
        };

        return new Rgba32((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}
