import { MessageCircleHeart, MessageSquare } from 'lucide-react'
import { type ComponentType } from 'react'
import { useTranslation } from 'react-i18next'

import { formatResetDate, useMessageLimits, type MessageAllowance } from '@/domains/messaging'
import { Card, ProgressBar, Skeleton } from '@/shared/ui'

/**
 * Сколько сообщений осталось на неделе (тикет «обновить логику сообщений»).
 *
 * Один блок на два экрана — профиль и мэтчи: остаток спрашивают в обоих
 * местах, и расходиться в цифрах или формулировках им нельзя.
 *
 * Показываем остаток, а не расход: человеку важно, сколько он ещё может, а не
 * сколько потратил. Цена сверх лимита стоит рядом — иначе исчерпанный лимит
 * читается как «всё, писать больше нельзя».
 */
export function MessageLimitsCard() {
  const { t } = useTranslation()

  const limits = useMessageLimits()

  if (limits.isPending) return <Skeleton className="h-28 w-full rounded-lg" />
  // Ошибку не показываем: это справка, а не действие. Экран, на котором она
  // живёт, ничего от неё не ждёт, и красная плашка тут только пугала бы.
  if (limits.isError) return null

  return (
    <Card padding="tight" className="flex flex-col gap-3">
      <AllowanceRow
        icon={MessageSquare}
        title={t('messages.limits.messages')}
        allowance={limits.data.message}
      />

      <AllowanceRow
        icon={MessageCircleHeart}
        title={t('messages.limits.superMessages')}
        allowance={limits.data.superMessage}
      />

      <p className="text-tiny text-faint">
        {t('messages.limits.afterLimit', {
          message: limits.data.message.cost,
          super: limits.data.superMessage.cost,
        })}
      </p>
    </Card>
  )
}

type AllowanceRowProps = {
  icon: ComponentType<{ className?: string; 'aria-hidden'?: boolean }>
  title: string
  allowance: MessageAllowance
}

function AllowanceRow({ icon: Icon, title, allowance }: AllowanceRowProps) {
  const { t, i18n } = useTranslation()

  const left = Math.max(0, allowance.remaining)

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between gap-2">
        <span className="flex items-center gap-2 text-base font-semibold">
          <Icon className="size-4 text-brand" aria-hidden />
          {title}
        </span>

        <span className="shrink-0 text-tiny text-muted-foreground">
          {t('messages.limits.left', { left, limit: allowance.limit })}
        </span>
      </div>

      <ProgressBar value={(left / allowance.limit) * 100} />

      <span className="text-tiny text-faint">
        {left > 0
          ? t('messages.limits.resetsAt', {
              date: formatResetDate(allowance.resetsAt, i18n.language),
            })
          : t('messages.limits.spent', {
              cost: allowance.cost,
              date: formatResetDate(allowance.resetsAt, i18n.language),
            })}
      </span>
    </div>
  )
}
