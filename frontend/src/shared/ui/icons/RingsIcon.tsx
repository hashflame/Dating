import { Icon, type IconProps } from './Icon'

/** Два кольца, одно с камнем — серьёзные отношения. */
export function RingsIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <circle cx="7.5" cy="15.4" r="5.5" />
      <circle cx="16.5" cy="15.4" r="5.5" />
      <path d="M7.5 9.9 L4.1 6.3 L5.8 3 h3.4 l1.7 3.3z" />
      <path d="M4.1 6.3 h6.8" />
    </Icon>
  )
}
