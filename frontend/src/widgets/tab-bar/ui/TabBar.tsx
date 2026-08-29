import { Link, useRouterState } from '@tanstack/react-router'
import { Heart, Lightbulb, Sparkles, UserRound, Zap } from 'lucide-react'
import { useState } from 'react'
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

/**
 * Экраны с меню, которые не принадлежат ни одной вкладке: кошелёк открывают
 * из шапки на любом экране. Подсветку на них оставляем там, где она была, —
 * иначе меню перепрыгивает на «Профиль», хотя человек никуда из своей
 * вкладки не уходил и вернётся стрелкой назад.
 */
const FLOATING_ROUTES: readonly string[] = [ROUTES.profileWallet]

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
 * Переключение анимировано на CSS, без `motion`: меню живёт в корневом
 * макете и попало бы в главный чанк вместе с библиотекой (~100 kB) — той
 * самой, которую ради этого держат внутри ленты. Двух переходов хватает:
 * подсветка переезжает между слотами, выбранная иконка пружинит.
 * `motion-reduce` гасит и то и другое.
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

  // Вложенные пути тоже принадлежат вкладке: у хаба мэтча путь `/matches/<id>`.
  const tabFromPath = FLOATING_ROUTES.includes(pathname)
    ? undefined
    : TABS.find(({ to }) => pathname === to || pathname.startsWith(`${to}/`))?.to

  const [lastTab, setLastTab] = useState<string>(ROUTES.feed)

  if (tabFromPath !== undefined && tabFromPath !== lastTab) {
    setLastTab(tabFromPath)
  }

  const activeTab = tabFromPath ?? lastTab

  const badges: Record<string, number> = {
    [ROUTES.likes]: unread.data?.likes ?? 0,
    [ROUTES.matches]: unread.data?.matches ?? 0,
  }

  // Позиция подсветки: слоты равной ширины, поэтому смещение — номер вкладки.
  const activeIndex = TABS.findIndex(({ to }) => to === activeTab)

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

        <div className="pointer-events-auto relative flex h-16 items-center rounded-full glass px-2">
          {/*
           * Подсветка вынесена из вкладки отдельным слоем: пока она живёт
           * внутри активной ссылки, при переключении она может только
           * исчезнуть здесь и появиться там. Отдельным слоем она едет —
           * связь «был там, стал тут» видно глазами, как в Telegram.
           *
           * Слой повторяет раскладку ряда (те же пять слотов по 20 % ширины
           * внутри тех же полей), поэтому позиция считается из номера
           * вкладки и не требует измерений DOM.
           */}
          <div
            className="pointer-events-none absolute inset-x-2 inset-y-0 flex items-center"
            aria-hidden
          >
            <div
              className="flex w-1/5 justify-center transition-transform duration-300 ease-spring motion-reduce:transition-none"
              style={{ transform: `translateX(${activeIndex * 100}%)` }}
            >
              <span className="size-12 rounded-md glass-pill" />
            </div>
          </div>

          {TABS.map(({ to, labelKey, Icon }, index) => {
            const active = activeIndex === index
            const badge = badges[to] ?? 0

            return (
              <Link
                key={to}
                to={to}
                // Роутер иначе считает вкладку активной и по префиксу пути и
                // сам ставит `aria-current`: на кошельке «текущими» оказались
                // бы сразу две вкладки. Подсветку решает только `active`.
                activeOptions={{ exact: true }}
                onClick={() => haptic.select()}
                aria-current={active ? 'page' : undefined}
                aria-label={t(labelKey)}
                className={cn(
                  // Ссылка занимает весь слот — палец попадает и мимо иконки, —
                  // а видимая часть остаётся прежним квадратом внутри.
                  'group relative flex h-12 flex-1 items-center justify-center outline-none',
                  'transition-colors duration-150',
                  active
                    ? 'text-brand'
                    : // Не `muted-foreground`: серый на просвечивающем стекле
                      // над контентом читался плохо. Берём цвет текста с
                      // прозрачностью — почти чёрный в светлой теме и почти
                      // белый в тёмной, но всё ещё тише активной вкладки.
                      'text-foreground/85 hover:text-foreground',
                )}
              >
                <span
                  className={cn(
                    // Скруглённый квадрат, а не круг: круглая подсветка внутри
                    // круглой пилюли читалась как ещё одна кнопка, квадрат же
                    // очевидно «подложка под иконкой». Тот же размер и радиус,
                    // что у едущей подсветки, — иначе она приедет мимо.
                    'relative flex size-12 items-center justify-center rounded-md',
                    'transition-colors duration-150',
                    'group-focus-visible:ring-[3px] group-focus-visible:ring-ring/40',
                    !active && 'hover:bg-accent',
                  )}
                >
                  {/* Иконки залиты, а не контурные: в пилюле они мелкие, и
                      контур на просвечивающем стекле читается хуже заливки.
                      При выборе иконка коротко пружинит — анимация
                      навешивается вместе с классом, то есть ровно в момент
                      переключения, и повторяется при каждом возврате. */}
                  <Icon
                    className={cn(
                      'size-6 fill-current stroke-current stroke-1',
                      active && 'motion-safe:animate-tab-pop',
                    )}
                    aria-hidden
                  />

                  {badge > 0 && (
                    <span className="absolute top-0.5 right-0 min-w-4 rounded-full bg-brand px-1 text-center text-micro leading-4 font-bold text-brand-foreground">
                      {badge > MAX_BADGE ? `${MAX_BADGE}+` : badge}
                    </span>
                  )}
                </span>
              </Link>
            )
          })}
        </div>
      </div>
    </nav>
  )
}
