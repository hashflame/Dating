namespace Blizka.App.DataExport;

/// <summary>Запрос на экспорт данных, ожидающий обработки в фоновой очереди (T-16.2).</summary>
public sealed record PendingDataExportRequest(Guid UserId);
