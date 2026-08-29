import { Icon, type IconProps } from './Icon'

/** Компас — попробовать что-то новое. */
export function CompassIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <circle cx="12" cy="12" r="10" />
      <path d="M15.9 8.1 L13.7 13.7 L8.1 15.9 L10.3 10.3z" />
    </Icon>
  )
}
