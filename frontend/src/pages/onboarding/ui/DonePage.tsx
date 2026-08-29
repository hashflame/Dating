import { useNavigate } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'

import { useCompletionResult } from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useHaptic } from '@/shared/telegram'
import { Button, Card, Logo, ProgressBar, SparkIcon } from '@/shared/ui'

/** Шаг 5 (S-07): начисление зорок и заполненность карточки. */
export function DonePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const result = useCompletionResult()

  // Две кнопки вели в одно и то же место — «Заполнить карточку» уносила в
  // ленту, ровно мимо цели. Теперь она открывает форму анкеты, а «Позже» —
  // ленту, как и обещает подпись.
  const goToProfile = (): void => {
    haptic.tap()
    void navigate({ to: ROUTES.profileEdit, replace: true })
  }

  const goToFeed = (): void => {
    haptic.tap()
    void navigate({ to: ROUTES.feed, replace: true })
  }

  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-4 px-5 text-center">
      <Logo size={60} />

      <h1 className="text-display font-bold">
        {t('onboarding.done.title')}
        <span className="mt-1 flex items-center justify-center gap-1.5 text-spark">
          <SparkIcon className="size-5" />
          {t('viewer.balance', { count: result?.sparksAwarded ?? 0 })}
        </span>
      </h1>

      <p className="max-w-[280px] text-base text-muted-foreground">
        {t('onboarding.done.description')}
      </p>

      {result && (
        <Card className="flex w-full flex-col gap-2 text-left">
          <p className="text-lg font-bold">
            {t('onboarding.done.completeness', { percent: result.profileCompleteness })}
          </p>

          <ProgressBar value={result.profileCompleteness} />

          {result.nextReward && (
            <p className="text-tiny text-faint">
              {t('onboarding.done.nextReward', {
                threshold: result.nextReward.threshold,
                sparks: result.nextReward.sparksReward,
              })}
            </p>
          )}
        </Card>
      )}

      <div className="mt-2 flex w-full flex-col gap-2">
        <Button size="lg" block onClick={goToProfile}>
          {t('onboarding.done.fillProfile')}
        </Button>

        <Button variant="ghost" size="lg" block onClick={goToFeed}>
          {t('onboarding.done.later')}
        </Button>
      </div>
    </main>
  )
}
