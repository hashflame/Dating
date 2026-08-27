import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { ToggleGroup, ToggleGroupItem } from './kit/toggle-group'

type SegmentedOption<TValue extends string> = {
  value: TValue
  label: string
  /** Иконка слева от подписи. */
  icon?: ReactNode
}

type SegmentedControlProps<TValue extends string> = {
  value: TValue
  onValueChange: (value: TValue) => void
  options: ReadonlyArray<SegmentedOption<TValue>>
  /** Название группы для читалок. */
  label: string
  /**
   * Разрешить снять выбор повторным тапом — тогда значением станет пустая
   * строка. Нужно там, где «не указано» это законное состояние поля анкеты:
   * отдельная кнопка «—» рядом с «Нет» читается как второй ответ, а не как
   * пустота.
   */
  allowDeselect?: boolean
  className?: string
}

/**
 * Взаимоисключающий выбор из двух-трёх вариантов: дорожка с «пилюлей».
 * Клавиатуру и роли держит radix, выбранное состояние — вариант `segment`.
 */
export function SegmentedControl<TValue extends string>({
  value,
  onValueChange,
  options,
  label,
  allowDeselect = false,
  className,
}: SegmentedControlProps<TValue>) {
  return (
    <ToggleGroup
      type="single"
      value={value}
      onValueChange={(next) => {
        if (next || allowDeselect) onValueChange(next as TValue)
      }}
      aria-label={label}
      // spacing > 0 — сегменты остаются отдельными «пилюлями»: при spacing=0
      // shadcn склеивает группу и скругляет только внешние углы крайних.
      spacing={1}
      className={cn('flex w-full rounded-full bg-track p-1', className)}
    >
      {options.map((option) => (
        <ToggleGroupItem
          key={option.value}
          value={option.value}
          variant="segment"
          size="none"
          // Обводка жирнее стандартной: в сегменте иконка мелкая и должна
          // читаться и в выбранном состоянии, и в приглушённом.
          className="h-10 flex-1 gap-1.5 rounded-full px-3 text-sm [&_svg]:stroke-[2.5]"
        >
          {option.icon}
          {option.label}
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  )
}
