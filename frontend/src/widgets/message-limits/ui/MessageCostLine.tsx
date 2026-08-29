import { useTranslation } from 'react-i18next'

import { formatResetDate, type MessageCharge } from '@/domains/messaging'
import { SparkIcon } from '@/shared/ui'

type MessageCostLineProps = {
  charge: MessageCharge
}

/**
 * Строка про остаток и цену прямо в шторке отправки: цену человек должен
 * видеть до нажатия, а не узнавать из списанного баланса.
 */
export function MessageCostLine({ charge }: MessageCostLineProps) {
  const { t, i18n } = useTranslation()

  return (
    <p className="flex items-center gap-1.5 text-tiny text-muted-foreground">
      <SparkIcon className="size-3.5" />

      {charge.free
        ? t('messages.cost.free', {
            left: charge.allowance.remaining,
            limit: charge.allowance.limit,
          })
        : t('messages.cost.paid', {
            cost: charge.cost,
            date: formatResetDate(charge.allowance.resetsAt, i18n.language),
          })}
    </p>
  )
}
