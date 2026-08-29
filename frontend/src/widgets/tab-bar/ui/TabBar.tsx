import { Link, useRouterState } from '@tanstack/react-router'
import { Heart, Lightbulb, Sparkles, UserRound, Zap } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { useUnreadNotifications } from '@/domains/notifications'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'

/*
 * Порядок из макета: лента ровно по центру, мэтчи первыми.
 *
 * Лента — то, ради чего открывают приложение, и в пилюле из пяти кружков
 * центральный достаётся большому пальцу без перехвата телефона. Остальные
 * сохранили прежний относительный порядок.
 */
const TABS = [
  { to: ROUTES.matches, labelKey: 'tabs.matches', Icon: Sparkles },
  { to: ROUTES.likes, labelKey: 'tabs.likes', Icon: Heart },
  { to: ROUTES.feed, labelKey: 'tabs.feed', Icon: Zap },
  { to: ROUTES.ideas, labelKey: 'tabs.ideas', Icon: Lightbulb },
  { to: ROUTES.profile, labelKey: 'tabs.profile', Icon: UserRound },
] as const satisfies ReadonlyArray<{ to: string; labelKey: string; Icon: unknown }>

/** Больше не показываем числом — дальше «99+». */
const MAX_BADGE = 99

/**
 * Нижнее меню разделов. Живёт в `RootLayout`, поэтому не дублируется
 * на каждом экране и не перерисовывается при переходах между вкладками.
 *
 * По макетам меню лежит поверх контента стеклянной «пилюлей» с размытым
 * свечением позади, а не полосой во всю ширину. Подписи убраны: пять слов
 * в пилюле не помещаются, и различать вкладки должны залитые иконки —
 * активная подсвечивается стеклянным квадратом (`glass-pill`) и фирменным
 * цветом самой иконки. Подписи остались для читалок.
 *
 * Бейджи на «Симпатиях» и «Мэтчах» приходят из `GET /api/notifications/unread` —
 * сервер считает только то, что появилось после `User.LastSeenLikesAt`/
 * `LastSeenMatchesAt` (T-10.2). Гасят их сами экраны: `LikesPage`/`MatchesPage`
 * зовут `POST /api/notifications/seen` при успешной загрузке своего списка.
 */
export function TabBar() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const unread = useUnreadNotifications()

  const badges: Record<string, number> = {
    [ROUTES.likes]: unread.data?.likes ?? 0,
    [ROUTES.matches]: unread.data?.matches ?? 0,
  }

  return (
    <nav
      aria-label={t('tabs.title')}
      className="pointer-events-none fixed inset-x-0 bottom-0 z-20 mx-auto w-full max-w-app px-5 pb-safe"
    >
      <div className="relative mb-4">
        {/* Пятно под стеклом: без него размывать нечего — см. `AppBar`. */}
        <div
          className="pointer-events-none absolute inset-x-10 -bottom-1 h-12 rounded-full bg-glow-ambient blur-2xl"
          aria-hidden
        />

        <div className="pointer-events-auto relative flex h-16 items-center justify-around gap-1 rounded-full glass px-2">
          {TABS.map(({ to, labelKey, Icon }) => {
            const active = pathname === to
            const badge = badges[to] ?? 0

            return (
              <Link
                key={to}
                to={to}
                onClick={() => haptic.select()}
                aria-current={active ? 'page' : undefined}
                aria-label={t(labelKey)}
                className={cn(
                  // Скруглённый квадрат, а не круг: круглая подсветка внутри
                  // круглой пилюли читалась как ещё одна кнопка, квадрат же
                  // очевидно «подложка под иконкой».
                  'relative flex size-12 shrink-0 items-center justify-center rounded-md transition-colors duration-150 outline-none',
                  'focus-visible:ring-[3px] focus-visible:ring-ring/40',
                  active
                    ? 'glass-pill text-brand'
                    : // Не `muted-foreground`: серый на просвечивающем стекле
                      // над контентом читался плохо. Берём цвет текста с
                      // прозрачностью — почти чёрный в светлой теме и почти
                      // белый в тёмной, но всё ещё тише активной вкладки.
                      'text-foreground/85 hover:bg-accent hover:text-foreground',
                )}
              >
                {/* Иконки залиты, а не контурные: в пилюле они мелкие, и
                    контур на просвечивающем стекле читается хуже заливки. */}
                <Icon className="size-6 fill-current stroke-current stroke-1" aria-hidden />

                {badge > 0 && (
                  <span className="absolute top-0.5 right-0 min-w-4 rounded-full bg-brand px-1 text-center text-micro leading-4 font-bold text-brand-foreground">
                    {badge > MAX_BADGE ? `${MAX_BADGE}+` : badge}
                  </span>
                )}
              </Link>
            )
          })}
        </div>
      </div>
    </nav>
  )
}
