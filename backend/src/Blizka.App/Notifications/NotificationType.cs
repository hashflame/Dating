namespace Blizka.App.Notifications;

/// <summary>Тип Telegram-уведомления (T-10.2) — определяет шаблон текста в <see cref="NotificationMessageCatalog"/>.</summary>
public enum NotificationType
{
    Match,
    NewProfiles,
    CityOpen,

    /// <summary>Партнёр ответил на вопрос дня — теперь видны оба ответа (T-11.1).</summary>
    QuestionOfDayBothAnswered,
}
