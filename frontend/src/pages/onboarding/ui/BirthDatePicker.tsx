import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/kit/select'

type BirthDatePickerProps = {
  /** `YYYY-MM-DD` или пустая строка, пока дата не выбрана полностью. */
  value: string
  onChange: (value: string) => void
}

const CURRENT_YEAR = new Date().getFullYear()
const MIN_YEAR = CURRENT_YEAR - 80
const MAX_YEAR = CURRENT_YEAR - 18

type Parts = { day: number | ''; month: number | ''; year: number | '' }

function parse(value: string): Parts {
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) return { day: '', month: '', year: '' }

  return { day, month, year }
}

function daysInMonth({ month, year }: Parts): number {
  if (!month) return 31

  return new Date(Number(year) || MAX_YEAR, month, 0).getDate()
}

function toIso({ day, month, year }: Parts): string {
  if (!day || !month || !year) return ''

  return `${String(year)}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

/**
 * Выбор даты рождения тремя списками вместо клавиатуры: на мобильном это быстрее
 * и без ошибок формата. Год ограничен так, чтобы нельзя было выбрать младше 18.
 *
 * Части даты держим внутри: наружу отдаём ISO только когда выбраны все три,
 * иначе первый же выбор терялся бы.
 */
export function BirthDatePicker({ value, onChange }: BirthDatePickerProps) {
  const { t, i18n } = useTranslation()
  const [parts, setParts] = useState<Parts>(() => parse(value))

  const monthNames = useMemo(() => {
    const format = new Intl.DateTimeFormat(i18n.language, { month: 'long' })

    return Array.from({ length: 12 }, (_, index) => format.format(new Date(2000, index, 1)))
  }, [i18n.language])

  const update = (patch: Partial<Parts>): void => {
    const next = { ...parts, ...patch }
    // 31 января → февраль: подрезаем день до последнего в месяце.
    const maxDay = daysInMonth(next)
    if (next.day && next.day > maxDay) next.day = maxDay

    setParts(next)
    onChange(toIso(next))
  }

  return (
    <div className="grid grid-cols-3 gap-2">
      <PartSelect
        label={t('onboarding.about.day')}
        value={parts.day}
        onChange={(day) => update({ day })}
        options={Array.from({ length: daysInMonth(parts) }, (_, index) => ({
          value: index + 1,
          label: String(index + 1),
        }))}
      />

      <PartSelect
        label={t('onboarding.about.month')}
        value={parts.month}
        onChange={(month) => update({ month })}
        options={monthNames.map((name, index) => ({ value: index + 1, label: name }))}
      />

      <PartSelect
        label={t('onboarding.about.year')}
        value={parts.year}
        onChange={(year) => update({ year })}
        options={Array.from({ length: MAX_YEAR - MIN_YEAR + 1 }, (_, index) => ({
          value: MAX_YEAR - index,
          label: String(MAX_YEAR - index),
        }))}
      />
    </div>
  )
}

type PartSelectProps = {
  label: string
  value: number | ''
  onChange: (value: number) => void
  options: Array<{ value: number; label: string }>
}

function PartSelect({ label, value, onChange, options }: PartSelectProps) {
  return (
    <Select
      value={value === '' ? undefined : String(value)}
      onValueChange={(next) => onChange(Number(next))}
    >
      <SelectTrigger
        aria-label={label}
        className="h-11 w-full justify-center gap-1 px-3"
      >
        <SelectValue placeholder={label} />
      </SelectTrigger>

      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={String(option.value)}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
