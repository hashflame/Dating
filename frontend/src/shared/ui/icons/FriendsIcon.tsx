import { Icon, type IconProps } from './Icon'

/** Двое рядом — дружба. */
export function FriendsIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <circle cx="8.6" cy="7.4" r="3.5" />
      <path d="M2 20.1 c0 -3.6 2.9 -6.3 6.5 -6.3 s6.5 2.7 6.5 6.3" />
      <path d="M16.7 4.9 a3.2 3.2 0 0 1 0 6.3" />
      <path d="M17.4 14 c2.8 0.6 4.6 2.8 4.6 6.1" />
    </Icon>
  )
}
