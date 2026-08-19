import { emitEvent, type ThemeParamsType } from '@tma.js/sdk-react'

/**
 * Цветовая схема в браузере: `system` следует настройке ОС/браузера,
 * `light` и `dark` — принудительное переключение для проверки цветов.
 *
 * Всё в этом файле работает только при `VITE_MOCK_TELEGRAM=1` в dev-сборке.
 * Внутри Telegram тему задаёт клиент, а он следует теме телефона.
 */
export type MockColorScheme = 'system' | 'light' | 'dark'

const STORAGE_KEY = 'blizka:dev-color-scheme'

const LIGHT_THEME: ThemeParamsType = {
  bg_color: '#ffffff',
  text_color: '#000000',
  hint_color: '#707579',
  link_color: '#168acd',
  button_color: '#2481cc',
  button_text_color: '#ffffff',
  secondary_bg_color: '#f4f4f5',
  header_bg_color: '#ffffff',
  bottom_bar_bg_color: '#f4f4f5',
  section_bg_color: '#ffffff',
  section_separator_color: '#e5e5e6',
  section_header_text_color: '#168acd',
  subtitle_text_color: '#707579',
  accent_text_color: '#168acd',
  destructive_text_color: '#e53935',
}

const DARK_THEME: ThemeParamsType = {
  bg_color: '#17212b',
  text_color: '#f5f5f5',
  hint_color: '#708499',
  link_color: '#6ab3f3',
  button_color: '#5288c1',
  button_text_color: '#ffffff',
  secondary_bg_color: '#232e3c',
  header_bg_color: '#17212b',
  bottom_bar_bg_color: '#232e3c',
  section_bg_color: '#232e3c',
  section_separator_color: '#111921',
  section_header_text_color: '#6ab3f3',
  subtitle_text_color: '#708499',
  accent_text_color: '#6ab3f3',
  destructive_text_color: '#ec3942',
}

const darkMedia = (): MediaQueryList => window.matchMedia('(prefers-color-scheme: dark)')

function isMockColorScheme(value: string | null): value is MockColorScheme {
  return value === 'system' || value === 'light' || value === 'dark'
}

/** Выбранный режим, включая `system`. */
export function getMockColorScheme(): MockColorScheme {
  const stored = localStorage.getItem(STORAGE_KEY)
  return isMockColorScheme(stored) ? stored : 'system'
}

/** Схема, которая реально применяется: `system` разворачивается в настройку ОС. */
export function resolveMockColorScheme(): 'light' | 'dark' {
  const scheme = getMockColorScheme()
  if (scheme !== 'system') return scheme

  return darkMedia().matches ? 'dark' : 'light'
}

/** Параметры темы для текущей схемы — ими кормим SDK. */
export function getMockThemeParams(): ThemeParamsType {
  return resolveMockColorScheme() === 'dark' ? DARK_THEME : LIGHT_THEME
}

/**
 * Переключает схему и сообщает SDK о смене темы тем же событием,
 * которое присылает настоящий клиент. Дальше всё происходит само:
 * SDK обновляет переменные `--tg-theme-*`, а вместе с ними и токены приложения.
 */
export function setMockColorScheme(scheme: MockColorScheme): void {
  localStorage.setItem(STORAGE_KEY, scheme)
  emitEvent('theme_changed', { theme_params: getMockThemeParams() })
}

/** Следит за темой ОС, пока выбран режим `system`. */
export function watchSystemColorScheme(): void {
  darkMedia().addEventListener('change', () => {
    if (getMockColorScheme() !== 'system') return

    emitEvent('theme_changed', { theme_params: getMockThemeParams() })
  })
}
