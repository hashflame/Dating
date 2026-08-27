/**
 * Кладёт текст в буфер обмена.
 *
 * Результат не ждём и ошибку глотаем: Clipboard API есть не во всех webview, а
 * подтверждение вызывающий показывает в любом случае — сам текст всегда виден
 * на экране, скопировать его можно руками.
 */
export function copyToClipboard(text: string): void {
  void navigator.clipboard?.writeText(text).catch(() => undefined)
}
