import { Link, useRouterState } from '@tanstack/react-router'
import { Heart, Lightbulb, Sparkles, UserRound, Zap } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { useUnreadNotifications } from '@/domains/notifications'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'

const TABS = [
  { to: ROUTES.feed, labelKey: 'tabs.feed', Icon: Zap },
  { to: ROUTES.likes, labelKey: 'tabs.likes', Icon: Heart },
  { to: ROUTES.matches, labelKey: 'tabs.matches', Icon: Sparkles },
  { to: ROUTES.ideas, labelKey: 'tabs.ideas', Icon: Lightbulb },
  { to: ROUTES.profile, labelKey: 'tabs.profile', Icon: UserRound },
] as const satisfies ReadonlyArray<{ to: string; labelKey: string; Icon: unknown }>

/** Больше не показываем числом — дальше «99+». */
const MAX_BADGE = 99

/**
 * Нижнее меню разделов. Живёт в `RootLayout`, поэтому не дублируется
 * на каждом экране и не перерисовывается при переходах между вкладками.
 *
 * Активную вкладку отмечает «пилюля» под иконкой, а не только цвет текста:
 * цветом одну иконку из пяти найти трудно, а залитая подложка читается сразу
 * и переживает тёмную тему. Подложка — `--brand-soft`, та же, что у выбранных
 * состояний на остальных экранах.
 *
 * Бейджи на «Симпатиях» и «Мэтчах» — непрочитанное с сервера
 * (`GET /api/notifications/unread`): это места, где что-то ждёт ответа.
 */
export function TabBar() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  // Что человек ещё не смотрел, считает сервер (T-10.2). Раньше бейдж
  // считался по общему числу входящих симпатий и висел всегда.
  const unread = useUnreadNotifications()

  const badges: Record<string, number> = {
    [ROUTES.likes]: unread.data?.likes ?? 0,
    [ROUTES.matches]: unread.data?.matches ?? 0,
  }

  return (
    <nav
      aria-label={t('tabs.title')}
      className="flex shrink-0 items-stretch justify-around border-t border-border bg-card/95 pb-safe backdrop-blur"
    >
      {TABS.map(({ to, labelKey, Icon }) => {
        const active = pathname === to
        const badge = badges[to] ?? 0

        return (
          <Link
            key={to}
            to={to}
            onClick={() => haptic.select()}
            aria-current={active ? 'page' : undefined}
            className="group flex min-h-15 flex-1 flex-col items-center justify-center gap-1 py-2 outline-none"
          >
            <span
              className={cn(
                'relative flex h-7 w-12 items-center justify-center rounded-full transition-colors duration-150',
                active ? 'bg-brand-soft' : 'group-hover:bg-accent',
              )}
            >
              <Icon
                className={cn(
                  'size-5 transition-colors duration-150',
                  active ? 'stroke-[2.5] text-brand' : 'text-muted-foreground',
                )}
                aria-hidden
              />

              {badge > 0 && (
                <span className="absolute -top-0.5 right-1 min-w-4 rounded-full bg-brand px-1 text-center text-micro leading-4 font-bold text-brand-foreground ring-2 ring-card">
                  {badge > MAX_BADGE ? `${MAX_BADGE}+` : badge}
                </span>
              )}
            </span>

            <span
              className={cn(
                'text-micro whitespace-nowrap transition-colors duration-150',
                active ? 'font-semibold text-brand' : 'text-muted-foreground',
              )}
            >
              {t(labelKey)}
            </span>
          </Link>
        )
      })}
    </nav>
  )
}
