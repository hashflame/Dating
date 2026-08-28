import { useTranslation } from 'react-i18next'

import { useHaptic } from '@/shared/telegram'
import { Card } from '@/shared/ui'
import { Sheet, SheetContent, SheetTitle } from '@/shared/ui/kit/sheet'
import { SwitchRow } from '@/shared/ui/SwitchRow'

type LikesSettingsSheetProps = {
  open: boolean
  onClose: () => void
  hideMatched: boolean
  onHideMatchedChange: (hideMatched: boolean) => void
}

/**
 * Настройки списка симпатий (кнопка в шапке).
 *
 * Переключатель уехал сюда из-под вкладок: строка с чекбоксом висела над
 * сеткой на каждом экране, хотя трогают её редко, — а сетка карточек и так
 * начинается сразу под вкладками. В шторке он лежит на серой подложке
 * карточки и выглядит как настройка, а не как случайная галочка.
 *
 * Значение применяется сразу, без кнопки «Применить»: фильтр локальный,
 * ничего не сохраняется на сервере.
 */
export function LikesSettingsSheet({
  open,
  onClose,
  hideMatched,
  onHideMatchedChange,
}: LikesSettingsSheetProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex flex-col gap-4 rounded-t-lg p-5 pb-safe-5"
      >
        <SheetTitle className="text-display font-bold">{t('likes.settings.title')}</SheetTitle>

        <Card padding="tight">
          <SwitchRow
            label={t('likes.hideMatched')}
            hint={t('likes.hideMatchedHint')}
            checked={hideMatched}
            onCheckedChange={(value) => {
              haptic.select()
              onHideMatchedChange(value)
            }}
          />
        </Card>
      </SheetContent>
    </Sheet>
  )
}
