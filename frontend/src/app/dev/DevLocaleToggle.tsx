import { useTranslation } from 'react-i18next'

import { SUPPORTED_LOCALES, type Locale } from '@/shared/i18n'
import { cn } from '@/shared/lib'

const LABELS: Record<Locale, string> = {
  ru: 'RU',
  be: 'BE',
  en: 'EN',
}

/**
 * Переключатель языка для проверки переводов в браузере.
 *
 * Сброс на перезагрузке — это нормально: в приложении язык приходит
 * из настроек Telegram-пользователя и вручную не выбирается.
 */
export function DevLocaleToggle() {
  const { i18n } = useTranslation()
  const current = i18n.language

  return (
    <div className="flex gap-1" role="group" aria-label="Язык (только для разработки)">
      {SUPPORTED_LOCALES.map((locale) => (
        <button
          key={locale}
          type="button"
          onClick={() => void i18n.changeLanguage(locale)}
          aria-pressed={current === locale}
          className={cn(
            'flex h-9 items-center justify-center rounded-full border border-border bg-card/90 px-2.5 text-tiny font-bold shadow-sm backdrop-blur transition-colors',
            current === locale
              ? 'bg-primary text-primary-foreground'
              : 'text-muted-foreground hover:bg-accent',
          )}
        >
          {LABELS[locale]}
        </button>
      ))}
    </div>
  )
}
