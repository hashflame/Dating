import { useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { setStoredLocale, SUPPORTED_LOCALES, type Locale } from '@/shared/i18n'
import { useHaptic } from '@/shared/telegram'
import { Card, ListRow } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'

type LanguageSheetProps = {
  open: boolean
  onClose: () => void
}

/** Ключи названий языков. Типизированный `t()` не принимает шаблонную строку. */
const LANGUAGE_KEYS = {
  ru: 'language.ru',
  be: 'language.be',
  en: 'language.en',
} as const satisfies Record<Locale, string>

/**
 * Выбор языка интерфейса.
 *
 * Названия языков написаны на них самих и одинаковы во всех локалях: человек,
 * которому приложение открылось не на его языке, ищет в списке знакомое слово,
 * а не перевод.
 *
 * Выбор применяется сразу и запоминается на устройстве (`setStoredLocale`).
 * Заодно сбрасываем кэш запросов: половина текстов приходит с сервера
 * (названия городов и интересов, подписи в кошельке, идеи свидания), и без
 * этого экран остался бы наполовину на прежнем языке до перезапуска.
 */
export function LanguageSheet({ open, onClose }: LanguageSheetProps) {
  const { t, i18n } = useTranslation()
  const haptic = useHaptic()
  const queryClient = useQueryClient()

  const change = (locale: Locale): void => {
    haptic.select()
    setStoredLocale(locale)

    void i18n.changeLanguage(locale).then(() => queryClient.invalidateQueries())
    onClose()
  }

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex flex-col gap-4 rounded-t-lg p-5 pb-safe-5"
      >
        <div className="flex flex-col gap-1 pr-10">
          <SheetTitle className="text-display font-bold">{t('language.title')}</SheetTitle>
          <SheetDescription className="text-tiny">{t('language.hint')}</SheetDescription>
        </div>

        <Card padding="none" className="overflow-hidden">
          {SUPPORTED_LOCALES.map((locale) => (
            <ListRow
              key={locale}
              title={t(LANGUAGE_KEYS[locale])}
              selected={i18n.language === locale}
              onClick={() => change(locale)}
            />
          ))}
        </Card>
      </SheetContent>
    </Sheet>
  )
}
