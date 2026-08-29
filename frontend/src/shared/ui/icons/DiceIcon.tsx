import { Icon, type IconProps } from './Icon'

/** Кубик — настолки и компания для хобби. */
export function DiceIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <rect x="2" y="2" width="20" height="20" rx="3" />
      <circle cx="7.6" cy="7.6" r="1.2" fill="currentColor" stroke="none" />
      <circle cx="12" cy="12" r="1.2" fill="currentColor" stroke="none" />
      <circle cx="16.4" cy="16.4" r="1.2" fill="currentColor" stroke="none" />
    </Icon>
  )
}
