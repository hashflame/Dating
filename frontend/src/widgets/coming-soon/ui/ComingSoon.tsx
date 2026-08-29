import { ChevronDown, Hammer } from 'lucide-react'
import { useState, type ComponentType } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Card } from '@/shared/ui'

/** Один пункт будущей функции: чем она будет полезна, а не как устроена. */
export type ComingSoonPoint = {
  icon: ComponentType<{ className?: string; 'aria-hidden'?: boolean }>
  title: string
  text: string
}

type ComingSoonProps = {
  title: string
  description: string
  points: readonly ComingSoonPoint[]
  /** Плотный вид — для секции внутри шторки, а не отдельного экрана. */
  compact?: boolean
  /** Список пунктов свёрнут: карточка не отжимает вниз то, чем пользуются сейчас. */
  collapsible?: boolean
}

/**
 * Функция в разработке: что именно готовится и зачем.
 *
 * Одна карточка на все такие места (идея свидания, AI-сообщения): пустой
 * экран с надписью «скоро» ничего не обещает и читается как поломка, а список
 * из трёх-четырёх понятных выгод — читается как план.
 *
 * Внутри нет ничего кликабельного, кроме сворачивания: обещать кнопку,
 * которая не работает, хуже, чем честно показать, что её пока нет.
 */
export function ComingSoon({
  title,
  description,
  points,
  compact = false,
  collapsible = false,
}: ComingSoonProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(!collapsible)

  const heading = (
    <div className="flex min-w-0 flex-col gap-2 text-left">
      <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-brand-soft px-2.5 py-1 text-micro font-bold tracking-wide text-brand uppercase">
        <Hammer className="size-3" aria-hidden />
        {t('comingSoon.badge')}
      </span>

      <h3 className={compact ? 'text-base font-bold' : 'text-display font-bold text-balance'}>
        {title}
      </h3>

      <p className="text-tiny text-muted-foreground">{description}</p>
    </div>
  )

  return (
    <Card padding={compact ? 'tight' : 'default'} className="flex flex-col gap-4">
      {collapsible ? (
        <button
          type="button"
          className="flex items-start gap-3 text-left"
          aria-expanded={open}
          onClick={() => setOpen((previous) => !previous)}
        >
          {heading}

          <ChevronDown
            className={cn(
              'mt-1 size-4 shrink-0 text-muted-foreground transition-transform',
              open && 'rotate-180',
            )}
            aria-hidden
          />
        </button>
      ) : (
        heading
      )}

      {open && (
        <ul className="flex flex-col gap-3">
          {points.map((point) => (
            <li key={point.title} className="flex gap-3">
              <span
                className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-strong"
                aria-hidden
              >
                <point.icon className="size-4 text-brand" />
              </span>

              <span className="flex min-w-0 flex-col gap-0.5">
                <span className="text-tiny font-semibold text-foreground">{point.title}</span>
                <span className="text-tiny text-muted-foreground">{point.text}</span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
