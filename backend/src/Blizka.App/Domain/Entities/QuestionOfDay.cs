namespace Blizka.App.Domain.Entities;

public sealed class QuestionOfDay
{
    public Guid Id { get; set; }

    public string TextRu { get; set; } = string.Empty;

    public string TextBe { get; set; } = string.Empty;

    public string TextEn { get; set; } = string.Empty;

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
