import { Check, Lock, Send } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useUnlockContact } from '@/domains/matches'
import { isApiError } from '@/shared/api'
import { useHaptic } from '@/shared/telegram'
import { Button, Card } from '@/shared/ui'

import { ComposeSheet } from './ComposeSheet'

type ContactBranchProps = {
  matchId: string
  /** Кому пишем: за анкетой идёт шторка сообщения, чтобы собрать заготовки. */
  userId: string
  name: string
  status: 'locked' | 'unlocked'
  cost: number
  /** Приходит только после оплаты. */
  telegramUsername: string | null
}

/**
 * Ветка «Написать»: открыть контакт за зорки (S-32), затем составить сообщение
 * по заготовке и уйти в Telegram (S-33).
 *
 * Сообщение бэкенд не отправляет и не должен: текст копируется в буфер, дальше
 * человек пишет сам в Telegram.
 */
export function ContactBranch({
  matchId,
  userId,
  name,
  status,
  cost,
  telegramUsername,
}: ContactBranchProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const unlock = useUnlockContact()
  const [error, setError] = useState<string | null>(null)
  const [composing, setComposing] = useState(false)

  const username = telegramUsername ?? unlock.data?.telegramUsername ?? null
  const link = username === null ? (unlock.data?.deepLink ?? null) : `https://t.me/${username}`
  const opened = status === 'unlocked' || unlock.isSuccess

  const handleUnlock = (): void => {
    haptic.tap()
    setError(null)
    unlock.mutate(matchId, {
      onSuccess: () => {
        haptic.success()
        setComposing(true)
      },
      onError: (reason) =>
        setError(
          isApiError(reason) && reason.status === 402
            ? t('feed.match.noSparks')
            : t('feed.match.unlockError'),
        ),
    })
  }

  if (!opened) {
    return (
      <Card padding="tight" className="flex flex-col gap-3">
        <span className="flex items-center gap-2 text-base font-semibold">
          <Lock className="size-4 text-brand" aria-hidden />
          {t('matches.contact.lockedTitle')}
        </span>

        <span className="text-tiny text-muted-foreground">
          {t('matches.contact.lockedHint', { name })}
        </span>

        <Button size="lg" block disabled={unlock.isPending} onClick={handleUnlock}>
          {t('matches.contact.unlock', { cost })}
        </Button>

        {error !== null && <span className="text-tiny text-destructive">{error}</span>}
      </Card>
    )
  }

  return (
    <>
      <Card padding="tight" className="flex flex-col gap-3">
        <span className="flex items-center gap-2 text-base font-semibold">
          <Check className="size-4 text-moss" aria-hidden />
          {username === null
            ? t('matches.contact.openedNoUsername')
            : t('matches.contact.opened', { username })}
        </span>

        <Button
          size="lg"
          block
          onClick={() => {
            haptic.tap()
            setComposing(true)
          }}
        >
          <Send aria-hidden />
          {t('feed.match.write')}
        </Button>
      </Card>

      <ComposeSheet
        open={composing}
        onClose={() => setComposing(false)}
        userId={userId}
        name={name}
        link={link}
      />
    </>
  )
}
