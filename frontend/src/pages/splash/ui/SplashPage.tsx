import { useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import { useOnboardingDraft } from '@/domains/onboarding'
import { resolveStartRoute, useSession } from '@/domains/session'
import { ErrorState, Logo, ProgressBar } from '@/shared/ui'

/**
 * Экран загрузки (S-01). Пока пользователь видит логотип, обмениваем
 * Telegram initData на сессию и по статусу решаем, куда вести дальше.
 */
export function SplashPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { data: session, isError, refetch } = useSession()

  // Новому пользователю нужен черновик: по нему решаем, продолжить анкету или
  // показать приветствие. Ошибку черновика не считаем блокирующей — начнём с нуля.
  const isNewUser = session?.status === 'new'
  const draft = useOnboardingDraft(isNewUser)
  const draftSettled = !isNewUser || draft.isSuccess || draft.isError

  useEffect(() => {
    if (!session || !draftSettled) return

    void navigate({ to: resolveStartRoute(session, draft.data?.step ?? 0), replace: true })
  }, [session, draftSettled, draft.data, navigate])

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
