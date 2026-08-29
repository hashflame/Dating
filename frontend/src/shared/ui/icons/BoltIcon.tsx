import { Icon, type IconProps } from './Icon'

/** Молния — недавняя активность. */
export function BoltIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M13.5 2 L5.4 13.5 h5.4 L10.5 22 L18.6 10.5 h-5.4z" />
    </Icon>
  )
}
