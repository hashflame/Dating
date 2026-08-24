namespace Blizka.App.UseCases.Matches;

/// <summary>Результат <c>POST /api/matches/{matchId}/unlock</c> (T-7.3, spec.md 9.1).</summary>
/// <param name="TelegramUsername">Telegram-логин второго участника — <c>null</c>, если у него не задан публичный username в Telegram.</param>
/// <param name="DeepLink"><c>https://t.me/{TelegramUsername}</c> — <c>null</c> вместе с <paramref name="TelegramUsername"/>.</param>
/// <param name="SparksSpent">Сколько зорок списано за этот вызов — <c>0</c>, если контакт уже был открыт ранее (идемпотентный повторный вызов, согласовано с пользователем при уточнении задачи).</param>
/// <param name="SparksBalance">Баланс зорок после операции.</param>
public sealed record UnlockContactResult(string? TelegramUsername, string? DeepLink, int SparksSpent, int SparksBalance);
