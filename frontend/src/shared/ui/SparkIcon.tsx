import { cn } from '@/shared/lib'

type SparkIconProps = {
  className?: string
}

/**
 * Зорка — знак внутренней валюты. Одна иконка на всё приложение: баланс,
 * кошелёк, цена сообщения.
 *
 * Форма и цвет специально не как у Telegram Stars: там пятиконечная звезда
 * золотого тона, здесь — четырёхлучевая зорка «Блізкі» в оранжевом. Валюты
 * лежат рядом в одном окне, и по иконке должно быть видно, чьи это деньги.
 *
 * Размер задаётся классом (`size-4`), как у иконок lucide, чтобы иконка
 * подставлялась в готовые строки без правки разметки.
 */
export function SparkIcon({ className }: SparkIconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn('size-4 shrink-0 text-spark', className)}
      aria-hidden
      focusable="false"
    >
      <path d="M12 0l2.6 9.4L24 12l-9.4 2.6L12 24l-2.6-9.4L0 12l9.4-2.6z" fill="currentColor" />
    </svg>
  )
}
