namespace Blizka.App.Notifications;

/// <summary>Тип Telegram-уведомления (T-10.2) — определяет шаблон текста в <see cref="NotificationMessageCatalog"/>.</summary>
public enum NotificationType
{
    Match,
    NewProfiles,
    CityOpen,

    /// <summary>Партнёр ответил на вопрос дня — теперь видны оба ответа (T-11.1).</summary>
    QuestionOfDayBothAnswered,

    /// <summary>Архив с данными пользователя собран и залит в хранилище (T-16.2) — <c>Placeholder</c> — ссылка на скачивание.</summary>
    DataExportReady,
}
