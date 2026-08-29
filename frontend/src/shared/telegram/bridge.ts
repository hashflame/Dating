import {
  openLink,
  openTelegramLink,
  retrieveLaunchParams,
  retrieveRawInitData,
} from '@tma.js/sdk-react'

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

/**
 * `start_param` из прямой ссылки (`t.me/bot/app?startapp=…`) — по нему видно,
 * откуда человек пришёл: инвайт, реклама, дип-линк. `undefined` — открыли
 * без параметра или не из Telegram.
 */
export function getStartParam(): string | undefined {
  return launchParams()?.tgWebAppStartParam
}

/** Клиент Telegram: `ios`, `android`, `tdesktop`, `weba`… `undefined` вне Telegram. */
export function getPlatform(): string | undefined {
  return launchParams()?.tgWebAppPlatform
}

/** Вне Telegram (и до инициализации SDK) обращение к launch params бросает. */
function launchParams(): ReturnType<typeof retrieveLaunchParams> | undefined {
  try {
    return retrieveLaunchParams()
  } catch {
    return undefined
  }
}

/**
 * Открывает ссылку так, чтобы мини-апп не выгружался. Нужна для перехода
 * в чат с мэтчем после открытия контакта.
 */
export function openExternalLink(url: string): void {
  if (openLink.isAvailable()) {
    openLink(url)
    return
  }

  window.open(url, '_blank', 'noopener,noreferrer')
}

/**
 * Открывает личку Telegram с человеком (`https://t.me/<username>`).
 *
 * Отдельно от `openExternalLink`: тот уводит во внешний браузер, а ссылку на
 * чат надо открыть внутри самого Telegram — иначе вместо диалога человек
 * видит веб-страницу профиля и кнопку «Открыть в приложении».
 */
export function openTelegramChat(url: string): void {
  if (openTelegramLink.isAvailable()) {
    openTelegramLink(url)
    return
  }

  openExternalLink(url)
}

/**
 * Отправляет ссылку в Telegram через штатный экран «Поделиться»: выбор чата
 * делает сам клиент, нам не нужны ни права, ни список контактов.
 */
export function shareToTelegram(url: string, text: string): void {
  const share = new URL('https://t.me/share/url')
  share.searchParams.set('url', url)
  share.searchParams.set('text', text)

  openExternalLink(share.toString())
}
