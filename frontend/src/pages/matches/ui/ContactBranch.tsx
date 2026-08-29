import { MessageSquareOff, Send } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useHaptic } from '@/shared/telegram'
import { Button, Card } from '@/shared/ui'
import { ComposeSheet } from '@/widgets/compose-sheet'

type ContactBranchProps = {
  matchId: string
  /** С кем открываем переписку. */
  userId: string
  name: string
  status: 'locked' | 'unlocked' | 'writes_first_only'
}

/**
 * Ветка «Написать» (S-32/S-33): переход в личку Telegram с готовым текстом.
 *
 * Контакт больше не продаётся за зорку: платит человек не за право написать,
 * а за переходы сверх недельного лимита — цену и остаток показывает шторка.
 * Юзернейм отсюда пропал не случайно: показывать его как трофей больше незачем,
 * кнопка и так ведёт в нужный диалог.
 *
 * `writes_first_only` — человек включил «Запретить писать мне» (S-51): кнопки
 * здесь нет вовсе. Это единственный случай, который виден заранее; остальные
 * отказы (удалился, заблокировал, нет юзернейма) выясняются только в момент
 * перехода, и о них говорит сама шторка.
 *
 * Текст без рода намеренно: макет S-51 обещает «пишет первой сама», но это
 * подпись в настройках самой женщины, а в хабе мэтча по ту сторону может быть
 * кто угодно — пола собеседника `MatchHubUserDto` не отдаёт.
 */
export function ContactBranch({ matchId, userId, name, status }: ContactBranchProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const [composing, setComposing] = useState(false)

  if (status === 'writes_first_only') {
    return (
      <Card padding="tight" className="flex flex-col gap-3">
        <span className="flex items-center gap-2 text-base font-semibold">
          <MessageSquareOff className="size-4 text-faint" aria-hidden />
          {t('matches.contact.writesFirstTitle')}
        </span>

        <span className="text-tiny text-muted-foreground">
          {t('matches.contact.writesFirstHint', { name })}
        </span>
      </Card>
    )
  }

  return (
    <>
      <Card padding="tight" className="flex flex-col gap-3">
        <span className="flex items-center gap-2 text-base font-semibold">
          <Send className="size-4 text-telegram" aria-hidden />
          {t('matches.contact.writeTitle')}
        </span>

        <span className="text-tiny text-muted-foreground">
          {t('matches.contact.writeHint', { name })}
        </span>

        <Button
          variant="telegram"
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
        kind="message"
        matchId={matchId}
      />
    </>
  )
}
