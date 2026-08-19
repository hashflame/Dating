import { openLink, retrieveRawInitData } from '@tma.js/sdk-react'

/**
 * Прямые вызовы Telegram без React и без инициализации SDK.
 * Отдельно от init.ts, чтобы `shared/api` не тянул за собой весь граф запуска.
 */

/** Сырая строка initData — то, что бэкенд проверяет по HMAC. */
export function getRawInitData(): string | undefined {
  try {
    return retrieveRawInitData()
  } catch {
    return undefined
  }
}

/** Открывает ссылку так, чтобы мини-апп не выгружался. */
export function openExternalLink(url: string): void {
  if (openLink.isAvailable()) {
    openLink(url)
    return
  }

  window.open(url, '_blank', 'noopener,noreferrer')
}
