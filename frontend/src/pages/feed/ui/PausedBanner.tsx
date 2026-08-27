import { PauseCircle } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { useResumeAccount } from '@/domains/viewer'
import { useHaptic } from '@/shared/telegram'
import { Button, Card } from '@/shared/ui'

/**
 * Аккаунт на паузе (S-51).
 *
 * Пауза убирает анкету из чужих лент, но сам человек ленту видит и свайпает как
 * обычно — без этой полосы он бы просто перестал получать мэтчи и не понял,
 * почему. Снять паузу можно прямо отсюда, а не идти обратно в настройки.
 */
export function PausedBanner() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const resume = useResumeAccount()

  return (
    <Card padding="tight" className="flex flex-col gap-2">
      <span className="flex items-center gap-2 text-base font-semibold">
        <PauseCircle className="size-4 text-brand" aria-hidden />
        {t('feed.paused.title')}
      </span>

      <span className="text-tiny text-muted-foreground">{t('feed.paused.description')}</span>

      <Button
        size="sm"
        block
        disabled={resume.isPending}
        onClick={() => {
          haptic.tap()
          resume.mutate()
        }}
      >
        {t('privacy.resume')}
      </Button>

      {resume.isError && (
        <span className="text-tiny text-destructive">{t('privacy.actionError')}</span>
      )}
    </Card>
  )
}
