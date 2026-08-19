const MINUTE = 60_000
const HOUR = 60 * MINUTE
const DAY = 24 * HOUR
const MONTH = 30 * DAY

/** Приводит значение из API (ISO-строка) к Date. */
export function parseIsoDate(value: string | Date): Date {
  return value instanceof Date ? value : new Date(value)
}

/** Полных лет на текущую дату. */
export function getAge(birthDate: string | Date, now: Date = new Date()): number {
  const birth = parseIsoDate(birthDate)
  const monthDiff = now.getMonth() - birth.getMonth()
  const isBeforeBirthday = monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())

  return now.getFullYear() - birth.getFullYear() - (isBeforeBirthday ? 1 : 0)
}

/**
 * Дата в человекочитаемом виде: «12 мая». Формы слов даёт Intl,
 * поэтому переводить ничего не нужно.
 */
export function formatDate(
  value: string | Date,
  locale: string,
  options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' },
): string {
  return new Intl.DateTimeFormat(locale, options).format(parseIsoDate(value))
}

/**
 * Относительное время: «5 минут назад», «вчера». Старше месяца — обычная дата.
 * Использовать через `useFormatters`, чтобы не передавать локаль руками.
 */
export function formatRelativeTime(
  value: string | Date,
  locale: string,
  now: Date = new Date(),
): string {
  const diff = parseIsoDate(value).getTime() - now.getTime()
  const abs = Math.abs(diff)
  const format = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })

  if (abs < MINUTE) return format.format(0, 'minute')
  if (abs < HOUR) return format.format(Math.round(diff / MINUTE), 'minute')
  if (abs < DAY) return format.format(Math.round(diff / HOUR), 'hour')
  if (abs < MONTH) return format.format(Math.round(diff / DAY), 'day')

  return formatDate(value, locale)
}
