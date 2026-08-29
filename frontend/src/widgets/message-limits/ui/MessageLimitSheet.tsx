import { useNavigate } from '@tanstack/react-router'
import { MessageCircleHeart } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { formatResetDate, type MessageCharge, type MessageKind } from '@/domains/messaging'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button, SparkIcon } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'

type MessageLimitSheetProps = {
  /** `null` — шторка закрыта. Держим вид сообщения снаружи ради анимации закрытия. */
  kind: MessageKind | null
  charge: MessageCharge
  sparksBalance: number
  pending: boolean
  /** Оплатить и отправить. Вызывается только когда зорок хватает. */
  onConfirm: () => void
  onClose: () => void
}

/**
 * Недельный лимит исчерпан (тикет «обновить логику сообщений»).
 *
 * Шторка появляется до отправки, а не после отказа сервера: списывать зорки
 * молча нельзя — человек должен увидеть цену и согласиться.
 *
 * Две ситуации, и они разные по смыслу: зорок хватает — предлагаем заплатить;
 * не хватает — платить нечем, и единственный честный выход отсюда это
 * кошелёк, где написано, как их заработать.
 */
export function MessageLimitSheet({
  kind,
  charge,
  sparksBalance,
  pending,
  onConfirm,
  onClose,
}: MessageLimitSheetProps) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const isSuper = kind === 'super'

  return (
    <Sheet open={kind !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex flex-col gap-4 rounded-t-xl border-0 px-5 pt-5 pb-safe-5"
      >
        <span
          className={cn(
            'flex size-11 items-center justify-center rounded-full',
            isSuper ? 'bg-brand-soft' : 'bg-spark/15',
          )}
          aria-hidden
        >
          {isSuper ? (
            <MessageCircleHeart className="size-5 text-brand" />
          ) : (
            <SparkIcon className="size-5" />
          )}
        </span>

        <div className="flex flex-col gap-1">
          <SheetTitle className="text-display font-bold">
            {isSuper ? t('messages.limit.superTitle') : t('messages.limit.title')}
          </SheetTitle>

          {/* У суперсообщения лимит — единица, и общий текст «использовали
              все 1» читался считалкой. Ему нужна своя формулировка. */}
          <SheetDescription className="text-base">
            {isSuper
              ? t('messages.limit.superSpentDescription', {
                  date: formatResetDate(charge.allowance.resetsAt, i18n.language),
                })
              : t('messages.limit.spentDescription', {
                  limit: charge.allowance.limit,
                  date: formatResetDate(charge.allowance.resetsAt, i18n.language),
                })}
          </SheetDescription>
        </div>

        {charge.affordable ? (
          <>
            <p className="text-base text-foreground">
              {t('messages.limit.payDescription', { cost: charge.cost, balance: sparksBalance })}
            </p>

            <Button
              size="lg"
              block
              disabled={pending}
              onClick={() => {
                haptic.tap()
                onConfirm()
              }}
            >
              {t('messages.limit.pay', { cost: charge.cost })}
            </Button>
          </>
        ) : (
          <>
            <p className="text-base text-foreground">
              {t('messages.limit.noSparks', { cost: charge.cost, balance: sparksBalance })}
            </p>

            <Button
              size="lg"
              block
              onClick={() => {
                haptic.tap()
                onClose()
                void navigate({ to: ROUTES.profileWallet })
              }}
            >
              <SparkIcon />
              {t('messages.limit.earn')}
            </Button>
          </>
        )}

        <Button variant="ghost" size="lg" block onClick={onClose}>
          {t('messages.limit.wait')}
        </Button>
      </SheetContent>
    </Sheet>
  )
}
