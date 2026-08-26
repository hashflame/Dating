namespace Blizka.App.DataExport;

/// <summary>Схема ключей архивов экспорта данных в S3-совместимом хранилище (T-16.2) — тот же бакет, что и фото, отдельный префикс.</summary>
public static class DataExportStorageKeys
{
    public static string Archive(Guid userId, Guid exportId) => $"exports/{userId:N}/{exportId:N}.json";
}
