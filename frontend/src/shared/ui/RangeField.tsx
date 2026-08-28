import { useState } from 'react'

import { cn } from '@/shared/lib'

import { Input } from './kit/input'
import { Slider } from './kit/slider'

type RangeFieldProps = {
  value: [number, number]
  onChange: (value: [number, number]) => void
  min: number
  max: number
  /** Суффикс внутри поля: «лет», «км». */
  suffix: string
  fromLabel: string
  toLabel: string
  className?: string
}

const clamp = (value: number, min: number, max: number): number =>
  Math.min(Math.max(value, min), max)

/**
 * Диапазон чисел: два поля с суффиксом плюс слайдер. Значение можно и ввести,
 * и подтянуть ползунком.
 *
 * Пока поле правят, оно показывает набранный текст: иначе ограничение диапазона
 * не давало бы набрать вторую цифру. Готовое значение уходит наружу сразу, как
 * только попадает в границы, — чтобы ввод не потерялся, если нажать «Далее»
 * не выходя из поля. На выходе из поля значение подтягивается к границам.
 */
export function RangeField({
  value,
  onChange,
  min,
  max,
  suffix,
  fromLabel,
  toLabel,
  className,
}: RangeFieldProps) {
  const [draft, setDraft] = useState<[string | null, string | null]>([null, null])

  /** Раздвигает границы, чтобы они не перехлёстывались. */
  const withOrder = (index: 0 | 1, next: number): [number, number] => {
    const result: [number, number] = [...value]
    result[index] = next

    if (index === 0 && result[0] >= result[1]) result[1] = Math.min(result[0] + 1, max)
    if (index === 1 && result[1] <= result[0]) result[0] = Math.max(result[1] - 1, min)

    return result
  }

  const handleInput = (index: 0 | 1, raw: string): void => {
    const digits = raw.replace(/\D/g, '').slice(0, 3)
    setDraft(index === 0 ? [digits, draft[1]] : [draft[0], digits])

    const parsed = Number.parseInt(digits, 10)
    // Промежуточный ввод («3» по пути к «30») наружу не отдаём.
    if (Number.isNaN(parsed) || parsed < min || parsed > max) return

    onChange(withOrder(index, parsed))
  }

  const handleBlur = (index: 0 | 1): void => {
    const raw = draft[index]
    setDraft([null, null])
    if (raw === null) return

    const parsed = Number.parseInt(raw, 10)
    if (Number.isNaN(parsed)) return

    onChange(withOrder(index, clamp(parsed, min, max)))
  }

  return (
    <div className={cn('flex flex-col gap-5', className)}>
      <div className="flex gap-2">
        <Bound
          label={fromLabel}
          suffix={suffix}
          value={draft[0] ?? String(value[0])}
          onInput={(raw) => handleInput(0, raw)}
          onCommit={() => handleBlur(0)}
        />
        <Bound
          label={toLabel}
          suffix={suffix}
          value={draft[1] ?? String(value[1])}
          onInput={(raw) => handleInput(1, raw)}
          onCommit={() => handleBlur(1)}
        />
      </div>

      <Slider
        value={value}
        onValueChange={([from, to]) => onChange([from ?? min, to ?? max])}
        min={min}
        max={max}
        step={1}
        minStepsBetweenThumbs={1}
        aria-label={`${fromLabel} — ${toLabel}`}
      />
    </div>
  )
}

type BoundProps = {
  label: string
  suffix: string
  value: string
  onInput: (raw: string) => void
  onCommit: () => void
}

function Bound({ label, suffix, value, onInput, onCommit }: BoundProps) {
  return (
    <div className="flex h-12 flex-1 items-center justify-center gap-1 rounded-md bg-surface transition-[box-shadow] focus-within:ring-[3px] focus-within:ring-ring/50">
      <Input
        value={value}
        onChange={(event) => onInput(event.target.value)}
        onBlur={onCommit}
        onKeyDown={(event) => {
          if (event.key === 'Enter') event.currentTarget.blur()
        }}
        inputMode="numeric"
        aria-label={label}
        className="h-auto w-10 bg-transparent p-0 text-right text-sm font-bold focus-visible:ring-0"
      />
      <span className="text-sm text-muted-foreground" aria-hidden>
        {suffix}
      </span>
    </div>
  )
}
