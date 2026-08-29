import { Icon, type IconProps } from './Icon'

/** Горы под солнцем — активный отдых на природе. */
export function MountainIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M2 20.1 L9 8.6 l3.9 6.3 l2.3 -3.3 l6.8 8.5z" />
      <circle cx="17" cy="6.2" r="2.4" />
    </Icon>
  )
}
