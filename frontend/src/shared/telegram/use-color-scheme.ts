import { miniApp, useSignal } from '@tma.js/sdk-react'

/**
 * Тёмная ли сейчас тема. Определяется по теме Telegram-клиента,
 * который следует настройке телефона.
 *
 * Для цветов это НЕ нужно: токены (`bg-background`, `text-foreground`, …)
 * переключаются сами. Хук нужен там, где от темы зависит не цвет,
 * а сам контент — картинка, иллюстрация, вариант анимации.
 */
export function useIsDarkTheme(): boolean {
  return useSignal(miniApp.isDark)
}
