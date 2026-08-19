import { cn } from '@/shared/lib'

type LogoProps = {
  /** Размер стороны в пикселях. */
  size?: number
  className?: string
}

/** Зорка — знак «Блізкі». Наследует цвет через `currentColor`. */
export function Logo({ size = 52, className }: LogoProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      className={cn('text-brand', className)}
      role="img"
      aria-label="Блізка"
    >
      <path d="M12 0l2.6 9.4L24 12l-9.4 2.6L12 24l-2.6-9.4L0 12l9.4-2.6z" fill="currentColor" />
    </svg>
  )
}
