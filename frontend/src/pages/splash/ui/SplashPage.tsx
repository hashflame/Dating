import { useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import { useConsentGiven, useOnboardingDraft } from '@/domains/onboarding'
import { isOnboardingSession, resolveStartRoute, useSession } from '@/domains/session'
import { ErrorState, Logo, ProgressBar } from '@/shared/ui'

/**
 * Экран загрузки (S-01). Пока пользователь видит логотип, обмениваем
 * Telegram initData на сессию и решаем, куда вести дальше.
 */
export function SplashPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { data: session, isError, refetch } = useSession()

  // Согласие и черновик нужны только тем, кто ещё проходит анкету: остальных
  // ведём в ленту, и лишние запросы там задерживали бы старт.
  const inOnboarding = session !== undefined && isOnboardingSession(session)
  const consent = useConsentGiven(inOnboarding)
  const draft = useOnboardingDraft(inOnboarding)

  // Ошибки этих двух запросов не блокируют старт: без ответа считаем, что
  // согласия нет и шагов нет, — пользователь просто начнёт с приветствия.
  const settled = (query: { isSuccess: boolean; isError: boolean }): boolean =>
    query.isSuccess || query.isError
  const ready = session !== undefined && (!inOnboarding || (settled(consent) && settled(draft)))

  useEffect(() => {
    if (!ready || session === undefined) return

    const route = resolveStartRoute({
      session,
      consentGiven: consent.data ?? false,
      completedSteps: draft.data?.step ?? 0,
    })

    void navigate({ to: route, replace: true })
  }, [ready, session, consent.data, draft.data, navigate])

  if (isError) {
    return (
      <main className="flex flex-1 items-center justify-center">
        <ErrorState
          title={t('splash.errorTitle')}
          description={t('splash.errorDescription')}
          onRetry={() => void refetch()}
        />
      </main>
    )
  }

  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 text-center">
      <Logo size={52} />

      <h1 className="mt-4 text-display font-bold">{t('app.name')}</h1>

      <p className="mt-2 text-base whitespace-pre-line text-muted-foreground">{t('app.tagline')}</p>

      <ProgressBar className="mt-6 w-[110px]" />
    </main>
  )
}
