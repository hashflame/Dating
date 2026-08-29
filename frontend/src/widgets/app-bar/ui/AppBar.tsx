import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'

import { ViewerBalance } from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { useHaptic } from '@/shared/telegram'

import { useAppBarAction } from '../model/app-bar-action-context'

/**
 * Верхняя панель разделов с нижним меню: одна и та же на всех вкладках.
 *
 * Живёт в обёртке роутера, а не на экранах: раньше баланс зорок рисовала
 * только лента, и при переходе на другую вкладку он исчезал — выглядело как
 * будто зорки потерялись.
 *
 * По макетам панель не занимает место в потоке, а лежит поверх контента
 * стеклянной «пилюлей»: основная зона экрана — сплошной фон без рамок, и
 * единственное цветное пятно на ней — размытое свечение за этим стеклом.
 * Отступ под панель контенту даёт утилита `pt-chrome`.
 */
export function AppBar() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const action = useAppBarAction()

  return (
    <header className="pointer-events-none fixed inset-x-0 top-0 z-20 mx-auto w-full max-w-app px-5 pt-safe">
      <div className="relative mt-4">
        {/* Пятно, которое размывает стекло. Лежит под панелью отдельным слоем:
            `backdrop-filter` берёт то, что нарисовано позади, — если убрать
            пятно, размывать будет нечего и панель станет обычной плашкой. */}
        <div
          className="pointer-events-none absolute inset-x-8 -top-2 h-14 rounded-full bg-glow-ambient blur-2xl"
          aria-hidden
        />

        <div className="pointer-events-auto relative flex h-14 items-center justify-between gap-2 rounded-full glass px-2">
          {/* Слот держит ширину всегда: без него название съезжало бы с центра
              на экранах, где действия нет. */}
          <span className="flex size-10 shrink-0 items-center justify-center">
            {action && (
              <button
                type="button"
                onClick={action.onClick}
                aria-label={action.label}
                className="flex size-10 items-center justify-center rounded-full bg-surface-strong text-foreground transition-colors duration-150 outline-none hover:bg-surface focus-visible:ring-[3px] focus-visible:ring-ring/40 active:scale-95"
              >
                <action.Icon className="size-5" aria-hidden />
              </button>
            )}
          </span>

          {/* Название центрируем по панели, а не по остатку строки: слева слот
              действия узкий и постоянный, справа — баланс, который шире и растёт
              вместе с числом. В потоке название уезжало бы влево тем сильнее,
              чем больше зорок. */}
          <span className="pointer-events-none absolute inset-x-0 text-center text-xl font-bold text-foreground">
            {t('app.name')}
          </span>

          {/* Баланс — вход в кошелёк: по звёздам жмут именно затем, чтобы
              разобраться, откуда число и как его увеличить.
              Ссылка оборачивает готовый элемент, а не живёт внутри него:
              `ViewerBalance` показывает баланс где угодно, а знание о том,
              куда ведёт клик, — это уже про навигацию приложения.
              Имя ссылки для читалок даёт подпись внутри («40 зорак»). */}
          <Link
            to={ROUTES.profileWallet}
            onClick={() => haptic.select()}
            className="shrink-0 rounded-full outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40 active:scale-95"
          >
            <ViewerBalance />
          </Link>
        </div>
      </div>
    </header>
  )
}
