import { useNavigate } from '@tanstack/react-router'
import { Sparkle } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useRecordConsent } from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useHaptic } from '@/shared/telegram'
import { Button } from '@/shared/ui'

import { WelcomeConsent } from './WelcomeConsent'
import { WelcomeHero } from './WelcomeHero'

const BENEFITS = [
  'welcome.benefits.matching',
  'welcome.benefits.firstMessage',
  'welcome.benefits.quickSignup',
] as const

/**
 * Приветствие и согласие (S-02) — первый экран нового пользователя.
 * Объясняет, чем приложение отличается, и фиксирует согласие с документами.
 */
export function WelcomePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const consent = useRecordConsent()
  const [accepted, setAccepted] = useState(false)

  const handleToggle = (checked: boolean): void => {
    setAccepted(checked)
    haptic.select()
  }

  const handleStart = (): void => {
    haptic.tap()

    consent.mutate(undefined, {
      onSuccess: () => {
        haptic.success()
        void navigate({ to: ROUTES.onboardingAbout, replace: true })
      },
      onError: () => haptic.error(),
    })
  }

  return (
    <main className="flex flex-1 flex-col gap-4 px-5 pt-4">
      <WelcomeHero />

      <h1 className="text-heading text-display text-balance">{t('welcome.title')}</h1>

      <ul className="flex flex-col gap-3">
        {BENEFITS.map((key) => (
          <li key={key} className="flex items-start gap-2.5">
            <Sparkle className="mt-1 size-4 shrink-0 text-brand" aria-hidden />
            <span className="text-base text-muted-foreground">{t(key)}</span>
          </li>
        ))}
      </ul>

      <div className="mt-auto flex flex-col gap-3 pb-4">
        {consent.isError && (
          <p className="text-center text-tiny text-destructive">{t('welcome.consent.error')}</p>
        )}

        <WelcomeConsent checked={accepted} onCheckedChange={handleToggle} />

        <Button size="lg" block disabled={!accepted || consent.isPending} onClick={handleStart}>
          {t('action.start')}
        </Button>
      </div>
    </main>
  )
}
