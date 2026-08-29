import { useTranslation } from 'react-i18next'

import { Button } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'

type UnsavedChangesSheetProps = {
  open: boolean
  onLeave: () => void
  onStay: () => void
}

/**
 * Предупреждение о несохранённых правках при выходе из формы (S-40).
 *
 * Уход по «Назад» — единственный способ потерять набранное: сохранение здесь
 * ручное, черновика нет. Поэтому спрашиваем прямо перед выходом.
 * «Остаться» стоит главной кнопкой, а уход — второстепенной: случайное
 * попадание по крупной кнопке должно возвращать в форму, а не стирать работу.
 */
export function UnsavedChangesSheet({ open, onLeave, onStay }: UnsavedChangesSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onStay()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        showCloseButton={false}
        className="rounded-t-xl border-0 px-4 pt-5 pb-safe-5"
      >
        <div className="flex flex-col gap-1">
          <SheetTitle className="text-lg font-bold">{t('profile.edit.leaveTitle')}</SheetTitle>
          <SheetDescription className="text-tiny">{t('profile.edit.leaveText')}</SheetDescription>
        </div>

        <div className="flex flex-col gap-2">
          <Button size="lg" block onClick={onStay}>
            {t('profile.edit.leaveStay')}
          </Button>

          <Button variant="ghost" size="lg" block onClick={onLeave}>
            {t('profile.edit.leaveConfirm')}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  )
}
