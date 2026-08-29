import { useEffect, useRef, type ReactNode } from 'react'

import { useSession } from '@/domains/session'
import { useViewer } from '@/domains/viewer'
import { identifyViewer, initAnalytics, screenFromPath, track } from '@/shared/analytics'
import { getPlatform, getStartParam, getTelegramUser } from '@/shared/telegram'

import { router } from '../router/router'

type AnalyticsProviderProps = {
  children: ReactNode
}

/** `app_opened` — ровно один на запуск, даже при двойном эффекте StrictMode. */
let appOpenedSent = false

/**
 * Последний отправленный экран. Снаружи хука: при повторном монтировании
 * (двойной эффект StrictMode) подписка заводится заново, и без общей памяти
 * первый экран уходил бы дважды.
 */
let lastScreen: string | null = null

/**
 * Инициализация аналитики, экраны и свойства человека.
 *
 * Живёт в `app`: только этот слой видит и роутер, и сессию, и профиль разом.
 * Сами события экранов и идентификация — единственное, что здесь есть;
 * продуктовые события стоят в доменных хуках, рядом со своими действиями.
 */
export function AnalyticsProvider({ children }: AnalyticsProviderProps) {
  const { data: session } = useSession()

  useEffect(() => {
    // Не ждём загрузки SDK: события, случившиеся раньше, он доберёт из очереди
    // (`shared/analytics/posthog.ts`), а первый экран не должен её дожидаться.
    void initAnalytics()

    if (appOpenedSent) return
    appOpenedSent = true

    track({
      name: 'app_opened',
      // `start_param` показывает, откуда человек пришёл: инвайт, реклама, дип-линк.
      start_param: getStartParam() ?? null,
      platform: getPlatform() ?? 'unknown',
    })
  }, [])

  useScreenViews()

  return (
    <>
      {/* Профиль запрашивается только у тех, кто прошёл приветствие: до согласия
          личность не идентифицируется, и лишний запрос на welcome не нужен. */}
      {session !== undefined && session.status !== 'new' && (
        <ViewerIdentity userId={session.userId} isNewUser={session.isNewUser} />
      )}

      {children}
    </>
  )
}

/**
 * `screen_viewed` по подписке на роутер. Имя экрана — ключ `ROUTES`, а не путь:
 * id мэтчей в аналитике не нужны.
 *
 * Текущий экран отправляется и при подписке: первое разрешение маршрута
 * случается до монтирования провайдера, и без этого `splash` терялся бы.
 */
function useScreenViews(): void {
  useEffect(() => {
    const send = (pathname: string): void => {
      const screen = screenFromPath(pathname)
      if (screen === null || screen === lastScreen) return

      lastScreen = screen
      track({ name: 'screen_viewed', screen })
    }

    send(router.state.location.pathname)

    return router.subscribe('onResolved', ({ toLocation }) => send(toLocation.pathname))
  }, [])
}

type ViewerIdentityProps = {
  userId: string
  isNewUser: boolean
}

/**
 * Свойства человека в PostHog: демография, платформа, состояние анкеты.
 * Ни имени, ни фото, ни `telegramId`, ни `instagramHandle` — только
 * перечисления и числа, по которым раскладываются графики.
 *
 * Отдельным компонентом, а не веткой в провайдере: профиль нужен не всем и
 * не сразу, а условный хук в React невозможен.
 */
function ViewerIdentity({ userId, isNewUser }: ViewerIdentityProps) {
  const { data: viewer } = useViewer()
  /** Что уже отправлено: identify зовём при изменении данных, а не на рендер. */
  const sent = useRef<string | null>(null)

  useEffect(() => {
    if (!viewer) return

    const properties = {
      age: viewer.age,
      gender: viewer.gender,
      city: viewer.cityName,
      locale: viewer.locale,
      status: viewer.status,
      photos_count: viewer.photos.length,
      interests_count: viewer.interests.length,
      profile_completeness: viewer.profileCompleteness,
      dating_goals: viewer.datingGoals,
      platform: getPlatform() ?? 'unknown',
      tg_language: getTelegramUser()?.languageCode ?? null,
      signup_is_new: isNewUser,
    }

    const snapshot = JSON.stringify(properties)
    if (sent.current === snapshot) return

    sent.current = snapshot
    identifyViewer(userId, properties)
  }, [viewer, userId, isNewUser])

  return null
}
